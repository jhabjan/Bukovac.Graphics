// SPDX-License-Identifier: GPL-3.0-or-later
//
// This file is part of Bukovac.Graphics project.
//
// Author: Josip Habjan (habjan@gmail.com, github: https://github.com/jhabjan)
// Copyright (c) 2026 Josip Habjan. All rights reserved.
//
// Bukovac.Graphics is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Bukovac.Graphics is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.

using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Bukovac.Graphics.Commands;
using Bukovac.Graphics.Rasterizers.Windows.Direct2D;

namespace Bukovac.Graphics.Rasterizers.Windows;

/// <summary>
/// Direct2D/DirectWrite hardware-accelerated rasterizer for Windows.
/// Uses an HWND render target for window presentation and a DC render target for off-screen / external HDC scenarios.
/// Text measurement and rendering uses DirectWrite for sub-pixel accuracy.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed unsafe class Direct2DRasterizer : IRasterizer
{
    private const int D2DERR_RECREATE_TARGET = unchecked((int)0x8899000C);

    // DXGI_FORMAT_B8G8R8A8_UNORM = 87
    private const uint DXGI_FORMAT_B8G8R8A8_UNORM = 87;

    private nint _d2dFactory;
    private nint _dwriteFactory;
    private nint _renderTarget;      // ID2D1HwndRenderTarget* or ID2D1DCRenderTarget*
    private nint _hwnd;
    private int _width;
    private int _height;
    private float _deviceDpi = 96f;
    private bool _disposed;
    private bool _isOffscreen;
    private bool _isExternalHdc;
    private bool _isDcRenderTarget;
    private int _viewportX;
    private int _viewportY;
    private nint _boundHdc;
    private nint _offscreenHdc;
    private nint _offscreenBitmap;
    private nint _offscreenBitmapOld;
    private nint _offscreenBitmapBits;
    private bool _inDrawSession;
    private Matrix3x2 _currentTransform = Matrix3x2.Identity;
    private Matrix3x2 _pixelToDipTransform = Matrix3x2.Identity;
    private D2D1_BITMAP_INTERPOLATION_MODE _bitmapInterpolationMode = D2D1_BITMAP_INTERPOLATION_MODE.LINEAR;
    private D2D1_ANTIALIAS_MODE _antialiasMode = D2D1_ANTIALIAS_MODE.PER_PRIMITIVE;
    private CompositingQuality _compositingQuality = CompositingQuality.Default;
    private float _pixelOffsetX;
    private float _pixelOffsetY;

    // Per-frame brush cache (cleared on EndFrame to match render target lifetime rules)
    private readonly Dictionary<uint, nint> _solidBrushes = new();

    // Stroke style cache: DashStyle → ID2D1StrokeStyle*
    private readonly Dictionary<DashStyle, nint> _strokeStyles = new();

    // D2D bitmap cache: ImageHandle.Id → ID2D1Bitmap*
    private readonly Dictionary<int, nint> _bitmapCache = new();

    // Clip state tracking
    private int _clipCount;
    private readonly Stack<int> _savedClipCounts = new();
    private readonly Stack<Matrix3x2> _savedTransforms = new();

    public void Initialize(NativeWindowHandle window, int width, int height)
    {
        if (window.Kind != "HWND")
            throw new ArgumentException($"Direct2DRasterizer requires HWND, got '{window.Kind}'");

        _hwnd = window.Handle;
        _isOffscreen = false;
        _viewportX = 0;
        _viewportY = 0;
        _width = width;
        _height = height;

        QueryDeviceDpi();
        UpdatePixelToDipTransform();
        CreateFactories();
        CreateHwndRenderTarget();
    }

    public void InitializeBitmap(int width, int height)
    {
        _isOffscreen = true;
        _viewportX = 0;
        _viewportY = 0;
        _width = width;
        _height = height;

        UpdatePixelToDipTransform();
        CreateFactories();
        CreateDcRenderTarget();
        CreateOffscreenDib(width, height);
        BindDcRenderTarget(_offscreenHdc);
    }

    public void InitializeFromHdc(nint hdc, int width, int height)
    {
        InitializeFromHdc(hdc, 0, 0, width, height);
    }

    public void InitializeFromHdc(nint hdc, int x, int y, int width, int height)
    {
        _isExternalHdc = true;
        _isOffscreen = true;
        _viewportX = x;
        _viewportY = y;
        _width = width;
        _height = height;

        UpdatePixelToDipTransform();
        CreateFactories();
        CreateDcRenderTarget();
        _boundHdc = hdc;
        BindDcRenderTarget(hdc);
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;

        if (_renderTarget == 0) return;

        if (!_isDcRenderTarget)
        {
            // HWND render target supports Resize directly
            var size = GetRenderTargetPixelSize();
            D2D1VTable.Resize((ID2D1RenderTarget*)_renderTarget, size);
        }
        else if (_isOffscreen && !_isExternalHdc)
        {
            DestroyOffscreenDib();
            CreateOffscreenDib(width, height);
            BindDcRenderTarget(_offscreenHdc);
        }
        else if (_boundHdc != 0)
        {
            BindDcRenderTarget(_boundHdc);
        }

        // For DC render targets, rebinding happens in BeginFrame
    }

    public void BeginFrame()
    {
        if (_renderTarget == 0) return;

        _currentTransform = _pixelToDipTransform;
        _clipCount = 0;
        _savedClipCounts.Clear();
        _savedTransforms.Clear();
        ReleaseBrushCache();

        D2D1VTable.BeginDraw((ID2D1RenderTarget*)_renderTarget);
        _inDrawSession = true;

        // Set text AA based on target type and compositing preference.
        var textAa = _compositingQuality switch
        {
            CompositingQuality.HighSpeed => D2D1_TEXT_ANTIALIAS_MODE.GRAYSCALE,
            _ => _hwnd != 0 ? D2D1_TEXT_ANTIALIAS_MODE.CLEARTYPE : D2D1_TEXT_ANTIALIAS_MODE.GRAYSCALE,
        };
        D2D1VTable.SetTextAntialiasMode((ID2D1RenderTarget*)_renderTarget, textAa);
        D2D1VTable.SetAntialiasMode((ID2D1RenderTarget*)_renderTarget, _antialiasMode);

        ApplyTransform(_currentTransform);
    }

    public void EndFrame(ReadOnlySpan<DrawCommand> commands)
    {
        if (_renderTarget == 0) return;

        if (!_inDrawSession)
        {
            D2D1VTable.BeginDraw((ID2D1RenderTarget*)_renderTarget);
            _inDrawSession = true;
        }

        ExecuteCommands(commands);

        // Pop any outstanding clips
        while (_clipCount > 0)
        {
            D2D1VTable.PopAxisAlignedClip((ID2D1RenderTarget*)_renderTarget);
            _clipCount--;
        }

        int hr = D2D1VTable.EndDraw((ID2D1RenderTarget*)_renderTarget);
        _inDrawSession = false;

        if (hr == D2DERR_RECREATE_TARGET)
        {
            RecreateRenderTarget();
        }

        ReleaseBrushCache();
    }

    public float GetDpi() => _deviceDpi;

    public void SetDpi(float dpi)
    {
        if (dpi > 0f)
            _deviceDpi = dpi;
        UpdatePixelToDipTransform();

        if (_renderTarget != 0)
        {
            D2D1VTable.SetDpi((ID2D1RenderTarget*)_renderTarget, _deviceDpi, _deviceDpi);
            if (!_isDcRenderTarget)
            {
                D2D1VTable.Resize((ID2D1RenderTarget*)_renderTarget, GetRenderTargetPixelSize());
            }
        }
    }

    public void ApplyQualitySettings(InterpolationMode interpolation, SmoothingMode smoothing,
        PixelOffsetMode pixelOffset, CompositingQuality compositing)
    {
        _bitmapInterpolationMode = interpolation switch
        {
            InterpolationMode.NearestNeighbor => D2D1_BITMAP_INTERPOLATION_MODE.NEAREST_NEIGHBOR,
            _ => D2D1_BITMAP_INTERPOLATION_MODE.LINEAR,
        };

        _antialiasMode = smoothing switch
        {
            SmoothingMode.None => D2D1_ANTIALIAS_MODE.ALIASED,
            _ => D2D1_ANTIALIAS_MODE.PER_PRIMITIVE,
        };

        _compositingQuality = compositing;

        (_pixelOffsetX, _pixelOffsetY) = pixelOffset switch
        {
            PixelOffsetMode.Half => (0.5f, 0.5f),
            _ => (0f, 0f),
        };

        if (_renderTarget != 0)
        {
            D2D1VTable.SetAntialiasMode((ID2D1RenderTarget*)_renderTarget, _antialiasMode);
        }
    }

    public Vector2 MeasureString(string text, FontSpec font, float maxWidth)
    {
        return MeasureStringCore(text, font, maxWidth);
    }

    public Vector2 MeasureString(string text, FontSpec font, float maxWidth, TextFormatFlags flags)
    {
        return MeasureStringCore(text, font, maxWidth, flags);
    }

    public float GetFontHeight(FontSpec font)
    {
        if (_dwriteFactory == 0) return 0f;

        nint textFormat = CreateTextFormat(font);
        if (textFormat == 0) return 0f;

        nint textLayout = 0;
        try
        {
            ReadOnlySpan<char> probe = "Mq";
            int hr = DWriteVTable.CreateTextLayout(
                (IDWriteFactory*)_dwriteFactory, probe, textFormat,
                float.MaxValue, float.MaxValue, out textLayout);
            if (hr < 0 || textLayout == 0) return 0f;

            hr = DWriteVTable.GetMetrics(textLayout, out var metrics);
            if (hr < 0) return 0f;

            return metrics.height;
        }
        finally
        {
            ComHelpers.Release(textLayout);
            ComHelpers.Release(textFormat);
        }
    }

    public bool TryCopyPixelsBgra(out int width, out int height, out byte[] bgraPixels)
    {
        width = _width;
        height = _height;
        if (_offscreenBitmapBits == 0 || _width <= 0 || _height <= 0)
        {
            width = 0;
            height = 0;
            bgraPixels = [];
            return false;
        }

        int byteCount = _width * _height * 4;
        bgraPixels = new byte[byteCount];
        Marshal.Copy(_offscreenBitmapBits, bgraPixels, 0, byteCount);
        return true;
    }

    // ── Command Execution ────────────────────────────────────────────────────

    private void ExecuteCommands(ReadOnlySpan<DrawCommand> commands)
    {
        for (int i = 0; i < commands.Length; i++)
        {
            ref readonly var cmd = ref commands[i];
            switch (cmd.Kind)
            {
                case DrawCommandKind.Clear:
                    ExecuteClear(cmd.Color);
                    break;
                case DrawCommandKind.Save:
                    ExecuteSave();
                    break;
                case DrawCommandKind.Restore:
                    ExecuteRestore();
                    break;
                case DrawCommandKind.SetClip:
                    ExecuteSetClip(in cmd);
                    break;
                case DrawCommandKind.ResetClip:
                    ExecuteResetClip();
                    break;
                case DrawCommandKind.DrawLine:
                    ExecuteDrawLine(in cmd);
                    break;
                case DrawCommandKind.DrawRectangle:
                    ExecuteDrawRectangle(in cmd);
                    break;
                case DrawCommandKind.FillRectangle:
                    ExecuteFillRectangle(in cmd);
                    break;
                case DrawCommandKind.DrawEllipse:
                    ExecuteDrawEllipse(in cmd);
                    break;
                case DrawCommandKind.FillEllipse:
                    ExecuteFillEllipse(in cmd);
                    break;
                case DrawCommandKind.DrawString:
                    ExecuteDrawString(in cmd);
                    break;
                case DrawCommandKind.SetTransform:
                    ExecuteSetTransform(in cmd);
                    break;
                case DrawCommandKind.ResetTransform:
                    ExecuteResetTransform();
                    break;
                case DrawCommandKind.DrawImage:
                    ExecuteDrawImage(in cmd);
                    break;
                case DrawCommandKind.DrawRoundedRectangle:
                    ExecuteDrawRoundedRectangle(in cmd);
                    break;
                case DrawCommandKind.FillRoundedRectangle:
                    ExecuteFillRoundedRectangle(in cmd);
                    break;
            }
        }
    }

    private void ExecuteClear(ColorF color)
    {
        if (_renderTarget == 0) return;
        D2D1VTable.Clear((ID2D1RenderTarget*)_renderTarget, ToD2DColor(color));
    }

    private void ExecuteSave()
    {
        _savedClipCounts.Push(_clipCount);
        _savedTransforms.Push(_currentTransform);
    }

    private void ExecuteRestore()
    {
        if (_savedClipCounts.Count == 0 || _renderTarget == 0) return;

        int savedCount = _savedClipCounts.Pop();
        while (_clipCount > savedCount)
        {
            D2D1VTable.PopAxisAlignedClip((ID2D1RenderTarget*)_renderTarget);
            _clipCount--;
        }

        _currentTransform = _savedTransforms.Count > 0
            ? _savedTransforms.Pop()
            : _pixelToDipTransform;
        ApplyTransform(_currentTransform);
    }

    private void ExecuteSetClip(in DrawCommand cmd)
    {
        if (_renderTarget == 0) return;
        var rect = ToD2DRect(cmd.Rect);
        D2D1VTable.PushAxisAlignedClip((ID2D1RenderTarget*)_renderTarget, rect);
        _clipCount++;
    }

    private void ExecuteResetClip()
    {
        if (_renderTarget == 0) return;

        // Pop all clips back to the save boundary
        int targetCount = _savedClipCounts.Count > 0 ? _savedClipCounts.Peek() : 0;
        while (_clipCount > targetCount)
        {
            D2D1VTable.PopAxisAlignedClip((ID2D1RenderTarget*)_renderTarget);
            _clipCount--;
        }
    }

    private void ExecuteDrawLine(in DrawCommand cmd)
    {
        if (_renderTarget == 0) return;

        nint brush = GetOrCreateBrush(cmd.Color);
        if (brush == 0) return;

        nint strokeStyle = GetOrCreateStrokeStyle(cmd.DashStyle);

        D2D1VTable.DrawLine(
            (ID2D1RenderTarget*)_renderTarget,
            new D2D1_POINT_2F(cmd.P1.X, cmd.P1.Y),
            new D2D1_POINT_2F(cmd.P2.X, cmd.P2.Y),
            brush,
            Math.Max(1f, cmd.StrokeWidth),
            strokeStyle);
    }

    private void ExecuteDrawRectangle(in DrawCommand cmd)
    {
        if (_renderTarget == 0) return;

        nint brush = GetOrCreateBrush(cmd.Color);
        if (brush == 0) return;

        nint strokeStyle = GetOrCreateStrokeStyle(cmd.DashStyle);
        float stroke = Math.Max(1f, cmd.StrokeWidth);

        D2D1VTable.DrawRectangle(
            (ID2D1RenderTarget*)_renderTarget,
            ToD2DRect(cmd.Rect),
            brush,
            stroke,
            strokeStyle);
    }

    private void ExecuteFillRectangle(in DrawCommand cmd)
    {
        if (_renderTarget == 0) return;

        nint brush = GetOrCreateBrush(cmd.Color);
        if (brush == 0) return;

        D2D1VTable.FillRectangle(
            (ID2D1RenderTarget*)_renderTarget,
            ToD2DRect(cmd.Rect),
            brush);
    }

    private void ExecuteDrawEllipse(in DrawCommand cmd)
    {
        if (_renderTarget == 0) return;

        nint brush = GetOrCreateBrush(cmd.Color);
        if (brush == 0) return;

        nint strokeStyle = GetOrCreateStrokeStyle(cmd.DashStyle);
        float stroke = Math.Max(1f, cmd.StrokeWidth);

        var center = new D2D1_POINT_2F(
            cmd.Rect.X + cmd.Rect.Width * 0.5f,
            cmd.Rect.Y + cmd.Rect.Height * 0.5f);
        var ellipse = new D2D1_ELLIPSE(center, cmd.Rect.Width * 0.5f, cmd.Rect.Height * 0.5f);

        D2D1VTable.DrawEllipse(
            (ID2D1RenderTarget*)_renderTarget,
            ellipse, brush, stroke, strokeStyle);
    }

    private void ExecuteFillEllipse(in DrawCommand cmd)
    {
        if (_renderTarget == 0) return;

        nint brush = GetOrCreateBrush(cmd.Color);
        if (brush == 0) return;

        var center = new D2D1_POINT_2F(
            cmd.Rect.X + cmd.Rect.Width * 0.5f,
            cmd.Rect.Y + cmd.Rect.Height * 0.5f);
        var ellipse = new D2D1_ELLIPSE(center, cmd.Rect.Width * 0.5f, cmd.Rect.Height * 0.5f);

        D2D1VTable.FillEllipse(
            (ID2D1RenderTarget*)_renderTarget,
            ellipse, brush);
    }

    private void ExecuteDrawRoundedRectangle(in DrawCommand cmd)
    {
        if (_renderTarget == 0) return;

        nint brush = GetOrCreateBrush(cmd.Color);
        if (brush == 0) return;

        nint strokeStyle = GetOrCreateStrokeStyle(cmd.DashStyle);
        float stroke = Math.Max(1f, cmd.StrokeWidth);

        var rr = new D2D1_ROUNDED_RECT(ToD2DRect(cmd.Rect), cmd.CornerRadius, cmd.CornerRadius);

        D2D1VTable.DrawRoundedRectangle(
            (ID2D1RenderTarget*)_renderTarget,
            rr, brush, stroke, strokeStyle);
    }

    private void ExecuteFillRoundedRectangle(in DrawCommand cmd)
    {
        if (_renderTarget == 0) return;

        nint brush = GetOrCreateBrush(cmd.Color);
        if (brush == 0) return;

        var rr = new D2D1_ROUNDED_RECT(ToD2DRect(cmd.Rect), cmd.CornerRadius, cmd.CornerRadius);

        D2D1VTable.FillRoundedRectangle(
            (ID2D1RenderTarget*)_renderTarget,
            rr, brush);
    }

    private void ExecuteDrawString(in DrawCommand cmd)
    {
        if (cmd.Text is null || cmd.Text.Length == 0) return;
        if (_renderTarget == 0 || _dwriteFactory == 0) return;

        nint brush = GetOrCreateBrush(cmd.Color);
        if (brush == 0) return;

        nint textFormat = CreateTextFormat(cmd.Font);
        if (textFormat == 0) return;

        try
        {
            float drawX = cmd.P1.X;
            if (cmd.GlyphAdvances is { Length: > 0 } advances)
            {
                int limit = Math.Min(advances.Length, cmd.Text.Length);
                int drawWidth = 0;
                for (int i = 0; i < limit; i++)
                {
                    drawWidth += advances[i];
                }

                if (cmd.TextAlignment != TextAlignment.Near && !float.IsPositiveInfinity(cmd.MaxWidth))
                {
                    float alignedWidth = cmd.MaxWidth;
                    if (cmd.TextAlignment == TextAlignment.Center)
                    {
                        drawX += (alignedWidth - drawWidth) / 2f;
                    }
                    else
                    {
                        drawX += alignedWidth - drawWidth;
                    }
                }

                // DirectWrite doesn't expose per-glyph DX like ExtTextOut.
                // Draw text elements one-by-one at explicit x positions to keep
                // spacing identical to GDI when caller provides advances.
                int codeUnit = 0;
                while (codeUnit < cmd.Text.Length)
                {
                    int glyphLen = 1;
                    if (char.IsHighSurrogate(cmd.Text[codeUnit]) &&
                        codeUnit + 1 < cmd.Text.Length &&
                        char.IsLowSurrogate(cmd.Text[codeUnit + 1]))
                    {
                        glyphLen = 2;
                    }

                    int glyphAdvance = 0;
                    int end = Math.Min(codeUnit + glyphLen, limit);
                    for (int i = codeUnit; i < end; i++)
                    {
                        glyphAdvance += advances[i];
                    }

                    var glyphRect = new D2D1_RECT_F(
                        drawX,
                        cmd.P1.Y,
                        drawX + Math.Max(1f, glyphAdvance),
                        cmd.P1.Y + 1_000_000f);

                    DWriteVTable.SetTextAlignment(textFormat, DWRITE_TEXT_ALIGNMENT.LEADING);
                    DWriteVTable.SetWordWrapping(textFormat, DWRITE_WORD_WRAPPING.NO_WRAP);

                    D2D1VTable.DrawText(
                        (ID2D1RenderTarget*)_renderTarget,
                        cmd.Text.AsSpan(codeUnit, glyphLen),
                        textFormat,
                        glyphRect,
                        brush,
                        D2D1_DRAW_TEXT_OPTIONS.ENABLE_COLOR_FONT);

                    drawX += glyphAdvance;
                    codeUnit += glyphLen;
                }

                return;
            }

            if (cmd.GlyphUniformAdvance > 0)
            {
                int drawWidth = cmd.GlyphUniformAdvance * cmd.Text.Length;
                if (cmd.TextAlignment != TextAlignment.Near && !float.IsPositiveInfinity(cmd.MaxWidth))
                {
                    float alignedWidth = cmd.MaxWidth;
                    if (cmd.TextAlignment == TextAlignment.Center)
                    {
                        drawX += (alignedWidth - drawWidth) / 2f;
                    }
                    else
                    {
                        drawX += alignedWidth - drawWidth;
                    }
                }

                int codeUnit = 0;
                while (codeUnit < cmd.Text.Length)
                {
                    int glyphLen = 1;
                    if (char.IsHighSurrogate(cmd.Text[codeUnit]) &&
                        codeUnit + 1 < cmd.Text.Length &&
                        char.IsLowSurrogate(cmd.Text[codeUnit + 1]))
                    {
                        glyphLen = 2;
                    }

                    int glyphAdvance = cmd.GlyphUniformAdvance * glyphLen;
                    var glyphRect = new D2D1_RECT_F(
                        drawX,
                        cmd.P1.Y,
                        drawX + Math.Max(1f, glyphAdvance),
                        cmd.P1.Y + 1_000_000f);

                    DWriteVTable.SetTextAlignment(textFormat, DWRITE_TEXT_ALIGNMENT.LEADING);
                    DWriteVTable.SetWordWrapping(textFormat, DWRITE_WORD_WRAPPING.NO_WRAP);

                    D2D1VTable.DrawText(
                        (ID2D1RenderTarget*)_renderTarget,
                        cmd.Text.AsSpan(codeUnit, glyphLen),
                        textFormat,
                        glyphRect,
                        brush,
                        D2D1_DRAW_TEXT_OPTIONS.ENABLE_COLOR_FONT);

                    drawX += glyphAdvance;
                    codeUnit += glyphLen;
                }

                return;
            }

            // Map Mestrovic TextAlignment to DirectWrite
            var dwAlignment = cmd.TextAlignment switch
            {
                TextAlignment.Center => DWRITE_TEXT_ALIGNMENT.CENTER,
                TextAlignment.Far => DWRITE_TEXT_ALIGNMENT.TRAILING,
                _ => DWRITE_TEXT_ALIGNMENT.LEADING,
            };
            DWriteVTable.SetTextAlignment(textFormat, dwAlignment);

            // Set wrapping mode
            bool noWrap = (cmd.TextFlags & TextFormatFlags.NoWrap) != 0 ||
                          float.IsPositiveInfinity(cmd.MaxWidth);
            DWriteVTable.SetWordWrapping(textFormat,
                noWrap ? DWRITE_WORD_WRAPPING.NO_WRAP : DWRITE_WORD_WRAPPING.WRAP);

            float layoutWidth = float.IsPositiveInfinity(cmd.MaxWidth) ? 1_000_000f : cmd.MaxWidth;
            float layoutHeight = 1_000_000f;

            var layoutRect = new D2D1_RECT_F(
                cmd.P1.X, cmd.P1.Y,
                cmd.P1.X + layoutWidth, cmd.P1.Y + layoutHeight);

            D2D1VTable.DrawText(
                (ID2D1RenderTarget*)_renderTarget,
                cmd.Text.AsSpan(),
                textFormat,
                layoutRect,
                brush,
                D2D1_DRAW_TEXT_OPTIONS.ENABLE_COLOR_FONT);
        }
        finally
        {
            ComHelpers.Release(textFormat);
        }
    }

    private void ExecuteSetTransform(in DrawCommand cmd)
    {
        if (_renderTarget == 0) return;
        _currentTransform = cmd.Transform * _pixelToDipTransform;
        var m = new D2D1_MATRIX_3X2_F(
            _currentTransform.M11, _currentTransform.M12,
            _currentTransform.M21, _currentTransform.M22,
            _currentTransform.M31, _currentTransform.M32);
        D2D1VTable.SetTransform((ID2D1RenderTarget*)_renderTarget, m);
    }

    private void ExecuteResetTransform()
    {
        if (_renderTarget == 0) return;
        _currentTransform = _pixelToDipTransform;
        ApplyTransform(_currentTransform);
    }

    private void ExecuteDrawImage(in DrawCommand cmd)
    {
        if (_renderTarget == 0) return;
        if (!ImageManager.TryGetImageData(cmd.Image, out var imgData)) return;

        nint bitmap = GetOrCreateD2DBitmap(cmd.Image, imgData);
        if (bitmap == 0) return;

        var destRect = ToD2DRect(cmd.Rect);

        D2D1_RECT_F srcRect;
        if (cmd.HasSrcRect)
        {
            srcRect = new D2D1_RECT_F(
                cmd.SrcRect.X, cmd.SrcRect.Y,
                cmd.SrcRect.Right, cmd.SrcRect.Bottom);
        }
        else
        {
            srcRect = new D2D1_RECT_F(0, 0, imgData.Width, imgData.Height);
        }

        float opacity = cmd.Opacity > 0f ? cmd.Opacity : 1f;

        D2D1VTable.DrawBitmap(
            (ID2D1RenderTarget*)_renderTarget,
            bitmap, destRect, opacity,
            _bitmapInterpolationMode,
            srcRect);
    }

    // ── Text Measurement (DirectWrite) ───────────────────────────────────────

    private Vector2 MeasureStringCore(string text, FontSpec font, float maxWidth, TextFormatFlags flags = TextFormatFlags.None)
    {
        if (_dwriteFactory == 0) return Vector2.Zero;
        if (string.IsNullOrEmpty(text)) return Vector2.Zero;

        nint textFormat = CreateTextFormat(font);
        if (textFormat == 0) return Vector2.Zero;

        nint textLayout = 0;
        try
        {
            bool noWrap = (flags & TextFormatFlags.NoWrap) != 0 ||
                          float.IsPositiveInfinity(maxWidth);
            DWriteVTable.SetWordWrapping(textFormat,
                noWrap ? DWRITE_WORD_WRAPPING.NO_WRAP : DWRITE_WORD_WRAPPING.WRAP);

            float w = float.IsPositiveInfinity(maxWidth) ? float.MaxValue : Math.Max(0, maxWidth);
            int hr = DWriteVTable.CreateTextLayout(
                (IDWriteFactory*)_dwriteFactory, text.AsSpan(), textFormat,
                w, float.MaxValue, out textLayout);
            if (hr < 0 || textLayout == 0) return Vector2.Zero;

            hr = DWriteVTable.GetMetrics(textLayout, out var metrics);
            if (hr < 0) return Vector2.Zero;

            var height = metrics.height;
            if (metrics.top < 0) height += -metrics.top;

            // Use widthIncludingTrailingWhitespace for consistency with GDI measurement
            return new Vector2(metrics.widthIncludingTrailingWhitespace, height);
        }
        finally
        {
            ComHelpers.Release(textLayout);
            ComHelpers.Release(textFormat);
        }
    }

    // ── Factory & Render Target Creation ─────────────────────────────────────

    private void CreateFactories()
    {
        if (_d2dFactory == 0)
        {
            int hr = D2D1.D2D1CreateFactory(
                D2D1_FACTORY_TYPE.SINGLE_THREADED,
                D2D1.IID_ID2D1Factory,
                0, out _d2dFactory);
            if (hr < 0)
                throw new InvalidOperationException($"D2D1CreateFactory failed: 0x{hr:X8}");
        }

        if (_dwriteFactory == 0)
        {
            int hr = DWrite.DWriteCreateFactory(
                DWRITE_FACTORY_TYPE.SHARED,
                DWrite.IID_IDWriteFactory,
                out _dwriteFactory);
            if (hr < 0)
                throw new InvalidOperationException($"DWriteCreateFactory failed: 0x{hr:X8}");
        }
    }

    private void CreateHwndRenderTarget()
    {
        var pixelFormat = new D2D1_PIXEL_FORMAT(DXGI_FORMAT_B8G8R8A8_UNORM, D2D1_ALPHA_MODE.PREMULTIPLIED);
        // Create with default (0) DPI — then override via SetDpi below.
        // Passing 96 in the properties struct is sometimes ignored by DPI-aware processes.
        var rtProps = new D2D1_RENDER_TARGET_PROPERTIES(
            D2D1_RENDER_TARGET_TYPE.DEFAULT, pixelFormat,
            0f, 0f, 0, 0);

        var hwndProps = new D2D1_HWND_RENDER_TARGET_PROPERTIES(
            _hwnd,
            GetRenderTargetPixelSize(),
            D2D1_PRESENT_OPTIONS.NONE);

        int hr = D2D1VTable.CreateHwndRenderTarget(
            (ID2D1Factory*)_d2dFactory,
            ref rtProps, ref hwndProps, out _renderTarget);

        if (hr < 0)
            throw new InvalidOperationException($"CreateHwndRenderTarget failed: 0x{hr:X8}");

        // Keep RT DPI synchronized with host DPI and map editor pixel coordinates
        // as DIPs (WinForms logical pixels at current monitor scale).
        D2D1VTable.SetDpi((ID2D1RenderTarget*)_renderTarget, _deviceDpi, _deviceDpi);

        _isDcRenderTarget = false;
    }

    private void CreateDcRenderTarget()
    {
        var pixelFormat = new D2D1_PIXEL_FORMAT(DXGI_FORMAT_B8G8R8A8_UNORM, D2D1_ALPHA_MODE.PREMULTIPLIED);
        var rtProps = new D2D1_RENDER_TARGET_PROPERTIES(
            D2D1_RENDER_TARGET_TYPE.DEFAULT, pixelFormat,
            0f, 0f, 0, 0);

        int hr = D2D1VTable.CreateDcRenderTarget(
            (ID2D1Factory*)_d2dFactory,
            ref rtProps, out _renderTarget);

        if (hr < 0)
            throw new InvalidOperationException($"CreateDcRenderTarget failed: 0x{hr:X8}");

        D2D1VTable.SetDpi((ID2D1RenderTarget*)_renderTarget, _deviceDpi, _deviceDpi);

        _isDcRenderTarget = true;
    }

    private void BindDcRenderTarget(nint hdc)
    {
        if (_renderTarget == 0) return;
        _boundHdc = hdc;

        var rect = new RECT
        {
            left = _viewportX,
            top = _viewportY,
            right = _viewportX + _width,
            bottom = _viewportY + _height,
        };
        D2D1VTable.BindDC((ID2D1DCRenderTarget*)_renderTarget, hdc, ref rect);
    }

    private void RecreateRenderTarget()
    {
        // Release all device-dependent resources
        ReleaseBrushCache();
        ReleaseBitmapCache();

        if (_renderTarget != 0)
        {
            ComHelpers.Release(_renderTarget);
            _renderTarget = 0;
        }

        if (!_isDcRenderTarget)
            CreateHwndRenderTarget();
        else
        {
            CreateDcRenderTarget();
            if (_boundHdc != 0)
            {
                BindDcRenderTarget(_boundHdc);
            }
        }
    }

    private void CreateOffscreenDib(int width, int height)
    {
        _offscreenHdc = D2DGdi32.CreateCompatibleDC(0);
        if (_offscreenHdc == 0)
            throw new InvalidOperationException("CreateCompatibleDC failed for Direct2D offscreen target.");

        D2D_BITMAPINFO bmi = new();
        bmi.bmiHeader.biSize = (uint)Marshal.SizeOf<D2D_BITMAPINFOHEADER>();
        bmi.bmiHeader.biWidth = width;
        bmi.bmiHeader.biHeight = -height; // top-down BGRA
        bmi.bmiHeader.biPlanes = 1;
        bmi.bmiHeader.biBitCount = 32;
        bmi.bmiHeader.biCompression = 0; // BI_RGB

        _offscreenBitmap = D2DGdi32.CreateDIBSection(
            _offscreenHdc, ref bmi, D2DGdi32.DIB_RGB_COLORS, out _offscreenBitmapBits, 0, 0);
        if (_offscreenBitmap == 0 || _offscreenBitmapBits == 0)
            throw new InvalidOperationException("CreateDIBSection failed for Direct2D offscreen target.");

        _offscreenBitmapOld = D2DGdi32.SelectObject(_offscreenHdc, _offscreenBitmap);
    }

    private void DestroyOffscreenDib()
    {
        if (_offscreenBitmapOld != 0 && _offscreenHdc != 0)
        {
            D2DGdi32.SelectObject(_offscreenHdc, _offscreenBitmapOld);
            _offscreenBitmapOld = 0;
        }

        if (_offscreenBitmap != 0)
        {
            D2DGdi32.DeleteObject(_offscreenBitmap);
            _offscreenBitmap = 0;
        }

        _offscreenBitmapBits = 0;

        if (_offscreenHdc != 0)
        {
            D2DGdi32.DeleteDC(_offscreenHdc);
            _offscreenHdc = 0;
        }
    }

    private void QueryDeviceDpi()
    {
        nint hdc = D2DUser32.GetDC(_hwnd);
        if (hdc != 0)
        {
            var dpi = D2DGdi32.GetDeviceCaps(hdc, D2DGdi32.LOGPIXELSY);
            _deviceDpi = dpi > 0 ? dpi : 96f;
            D2DUser32.ReleaseDC(_hwnd, hdc);
        }
    }

    private D2D1_SIZE_U GetRenderTargetPixelSize()
    {
        int w = _width;
        int h = _height;
        if (_hwnd != 0 && D2DUser32.GetClientRect(_hwnd, out RECT rc))
        {
            w = Math.Max(1, rc.right - rc.left);
            h = Math.Max(1, rc.bottom - rc.top);
        }

        uint pxWidth = (uint)Math.Max(1, w);
        uint pxHeight = (uint)Math.Max(1, h);
        return new D2D1_SIZE_U(pxWidth, pxHeight);
    }

    private void UpdatePixelToDipTransform()
    {
        float dpi = _deviceDpi > 0f ? _deviceDpi : 96f;
        float scale = 96f / dpi;
        _pixelToDipTransform = Math.Abs(scale - 1f) < 0.0001f
            ? Matrix3x2.Identity
            : Matrix3x2.CreateScale(scale);
    }

    // ── Resource Helpers ─────────────────────────────────────────────────────

    private nint GetOrCreateBrush(ColorF color)
    {
        if (_renderTarget == 0) return 0;

        uint key = color.ToArgb32();
        if (_solidBrushes.TryGetValue(key, out var brush) && brush != 0)
            return brush;

        int hr = D2D1VTable.CreateSolidColorBrush(
            (ID2D1RenderTarget*)_renderTarget,
            ToD2DColor(color), out brush);
        if (hr < 0 || brush == 0) return 0;

        _solidBrushes[key] = brush;
        return brush;
    }

    // Custom dash patterns matching GDI cosmetic pen styles (in stroke-width multiples).
    // D2D built-in patterns are too small at thin widths; these give parity with GDI.
    private static ReadOnlySpan<float> GdiDashPattern => [18f, 6f];
    private static ReadOnlySpan<float> GdiDotPattern => [3f, 3f];
    private static ReadOnlySpan<float> GdiDashDotPattern => [9f, 6f, 3f, 6f];
    private static ReadOnlySpan<float> GdiDashDotDotPattern => [9f, 3f, 3f, 3f, 3f, 3f];

    private nint GetOrCreateStrokeStyle(DashStyle style)
    {
        if (style == DashStyle.Solid || _d2dFactory == 0)
            return 0;

        if (_strokeStyles.TryGetValue(style, out var existing) && existing != 0)
            return existing;

        ReadOnlySpan<float> pattern = style switch
        {
            DashStyle.Dash => GdiDashPattern,
            DashStyle.Dot => GdiDotPattern,
            DashStyle.DashDot => GdiDashDotPattern,
            DashStyle.DashDotDot => GdiDashDotDotPattern,
            _ => default,
        };

        int hr;
        nint strokeStyle;

        if (pattern.IsEmpty)
        {
            var props = new D2D1_STROKE_STYLE_PROPERTIES(
                D2D1_CAP_STYLE.FLAT, D2D1_CAP_STYLE.FLAT, D2D1_CAP_STYLE.FLAT,
                D2D1_LINE_JOIN.MITER, 10f, D2D1_DASH_STYLE.SOLID, 0f);
            hr = D2D1VTable.CreateStrokeStyle((ID2D1Factory*)_d2dFactory, props, out strokeStyle);
        }
        else
        {
            var props = new D2D1_STROKE_STYLE_PROPERTIES(
                D2D1_CAP_STYLE.FLAT, D2D1_CAP_STYLE.FLAT, D2D1_CAP_STYLE.FLAT,
                D2D1_LINE_JOIN.MITER, 10f, D2D1_DASH_STYLE.CUSTOM, 0f);
            fixed (float* pDashes = pattern)
            {
                hr = D2D1VTable.CreateStrokeStyle(
                    (ID2D1Factory*)_d2dFactory, props,
                    pDashes, (uint)pattern.Length,
                    out strokeStyle);
            }
        }

        if (hr < 0) return 0;

        _strokeStyles[style] = strokeStyle;
        return strokeStyle;
    }

    private nint GetOrCreateD2DBitmap(ImageHandle handle, ImageData imgData)
    {
        if (_renderTarget == 0) return 0;

        if (_bitmapCache.TryGetValue(handle.Id, out var existing) && existing != 0)
            return existing;

        var pixelFormat = new D2D1_PIXEL_FORMAT(DXGI_FORMAT_B8G8R8A8_UNORM, D2D1_ALPHA_MODE.PREMULTIPLIED);
        var bitmapProps = new D2D1_BITMAP_PROPERTIES(pixelFormat, 96f, 96f);
        var size = new D2D1_SIZE_U((uint)imgData.Width, (uint)imgData.Height);

        nint bitmap;
        fixed (byte* pPixels = imgData.BgraPixels)
        {
            int hr = D2D1VTable.CreateBitmap(
                (ID2D1RenderTarget*)_renderTarget,
                size, (nint)pPixels, (uint)imgData.Stride,
                bitmapProps, out bitmap);
            if (hr < 0 || bitmap == 0) return 0;
        }

        _bitmapCache[handle.Id] = bitmap;
        return bitmap;
    }

    private nint CreateTextFormat(FontSpec font)
    {
        if (_dwriteFactory == 0) return 0;

        var weight = (DWRITE_FONT_WEIGHT)(int)font.Weight;
        var style = font.Style == FontStyle.Italic
            ? DWRITE_FONT_STYLE.ITALIC
            : DWRITE_FONT_STYLE.NORMAL;

        // FontSpec.Size is in points. DirectWrite expects DIPs (= points at 96 DPI).
        // Points → DIPs: sizePt * (96 / 72) = sizePt * 4/3.
        // However, if the font size in the Canvas world is already in logical px
        // (which it is in GdiRasterizer — it converts pt to px at 96 DPI),
        // we match that behavior: emSize = sizePt * 96/72.
        float emSize = font.Size * (96f / 72f);
        if (emSize <= 0) emSize = 1f;

        int hr = DWriteVTable.CreateTextFormat(
            (IDWriteFactory*)_dwriteFactory,
            font.FamilyName,
            weight, style, emSize,
            out nint textFormat);

        return hr < 0 ? 0 : textFormat;
    }

    // ── Conversion Helpers ───────────────────────────────────────────────────

    private static D2D1_COLOR_F ToD2DColor(ColorF c) =>
        new(c.R, c.G, c.B, c.A);

    private static D2D1_RECT_F ToD2DRect(RectF r) =>
        new(r.X, r.Y, r.Right, r.Bottom);

    private void ApplyTransform(in Matrix3x2 transform)
    {
        var m = new D2D1_MATRIX_3X2_F(
            transform.M11, transform.M12,
            transform.M21, transform.M22,
            transform.M31 + _pixelOffsetX, transform.M32 + _pixelOffsetY);
        D2D1VTable.SetTransform((ID2D1RenderTarget*)_renderTarget, m);
    }

    // ── Cleanup ──────────────────────────────────────────────────────────────

    private void ReleaseBrushCache()
    {
        foreach (var (_, brush) in _solidBrushes)
            ComHelpers.Release(brush);
        _solidBrushes.Clear();
    }

    private void ReleaseBitmapCache()
    {
        foreach (var (_, bmp) in _bitmapCache)
            ComHelpers.Release(bmp);
        _bitmapCache.Clear();
    }

    private void ReleaseStrokeStyles()
    {
        foreach (var (_, ss) in _strokeStyles)
            ComHelpers.Release(ss);
        _strokeStyles.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        ReleaseBrushCache();
        ReleaseBitmapCache();
        ReleaseStrokeStyles();
        DestroyOffscreenDib();

        if (_renderTarget != 0)
        {
            ComHelpers.Release(_renderTarget);
            _renderTarget = 0;
        }

        if (_dwriteFactory != 0)
        {
            ComHelpers.Release(_dwriteFactory);
            _dwriteFactory = 0;
        }

        if (_d2dFactory != 0)
        {
            ComHelpers.Release(_d2dFactory);
            _d2dFactory = 0;
        }
    }
}
