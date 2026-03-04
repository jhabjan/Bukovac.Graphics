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
using Bukovac.Graphics.Rasterizers.Windows.GDI;

namespace Bukovac.Graphics.Rasterizers.Windows;

/// <summary>
/// GDI-based CPU rasterizer for Windows. Uses a DIB section for double buffering.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class GdiRasterizer : IRasterizer
{
    private nint _hwnd;
    private nint _hdcWindow;
    private nint _hdcMem;
    private nint _hBitmap;
    private nint _hBitmapOld;
    private nint _bitmapBits;
    private int _width;
    private int _height;
    private bool _disposed;
    private bool _isOffscreen;
    private bool _isExternalHdc;
    private int _viewportX;
    private int _viewportY;
    private float _deviceDpi = 96f;
    private Matrix3x2 _currentTransform = Matrix3x2.Identity;
    private readonly Stack<Matrix3x2> _savedTransforms = new();
    private int _stretchMode = Gdi32.COLORONCOLOR;
    private SmoothingMode _smoothingMode = SmoothingMode.Default;
    private CompositingQuality _compositingQuality = CompositingQuality.Default;
    private uint _fontQuality = Gdi32.CLEARTYPE_QUALITY;
    private float _pixelOffsetX;
    private float _pixelOffsetY;
    private readonly Dictionary<FontSpec, nint> _fontCache = new();

    public void Initialize(NativeWindowHandle window, int width, int height)
    {
        if (window.Kind != "HWND")
            throw new ArgumentException($"GdiRasterizer requires HWND, got '{window.Kind}'");

        _hwnd = window.Handle;
        _isOffscreen = false;
        _viewportX = 0;
        _viewportY = 0;
        _hdcWindow = User32.GetDC(_hwnd);
        QueryDeviceDpi();
        CreateBackBuffer(width, height);
        Gdi32.SetGraphicsMode(_hdcMem, Gdi32.GM_ADVANCED);
    }

    public void InitializeFromHdc(nint hdc, int width, int height)
    {
        InitializeFromHdc(hdc, 0, 0, width, height);
    }

    public void InitializeFromHdc(nint hdc, int x, int y, int width, int height)
    {
        _hdcMem = hdc;
        _width = width;
        _height = height;
        _viewportX = x;
        _viewportY = y;
        _isExternalHdc = true;
        _isOffscreen = true;
        Gdi32.SetGraphicsMode(_hdcMem, Gdi32.GM_ADVANCED);
    }

    public void InitializeBitmap(int width, int height)
    {
        _isOffscreen = true;
        _viewportX = 0;
        _viewportY = 0;
        _hdcWindow = 0;
        nint hdcScreen = User32.GetDC(0);
        _hdcMem = Gdi32.CreateCompatibleDC(hdcScreen);
        User32.ReleaseDC(0, hdcScreen);
        CreateDIBSection(width, height);
        Gdi32.SetGraphicsMode(_hdcMem, Gdi32.GM_ADVANCED);
    }

    public void Resize(int width, int height)
    {
        DestroyBackBuffer();
        CreateBackBuffer(width, height);
    }

    public void BeginFrame()
    {
        _currentTransform = Matrix3x2.CreateTranslation(_viewportX + _pixelOffsetX, _viewportY + _pixelOffsetY);
        _savedTransforms.Clear();
        var xform = new XFORM
        {
            eM11 = _currentTransform.M11,
            eM12 = _currentTransform.M12,
            eM21 = _currentTransform.M21,
            eM22 = _currentTransform.M22,
            eDx = _currentTransform.M31,
            eDy = _currentTransform.M32,
        };
        Gdi32.SetWorldTransform(_hdcMem, ref xform);

        ApplyViewportClip();
    }

    public void EndFrame(ReadOnlySpan<DrawCommand> commands)
    {
        ExecuteCommands(commands);

        if (!_isOffscreen && _hwnd != 0)
        {
            // Blit the back buffer to the window
            nint hdcWindow = User32.GetDC(_hwnd);
            Gdi32.BitBlt(hdcWindow, 0, 0, _width, _height, _hdcMem, 0, 0, Gdi32.SRCCOPY);
            User32.ReleaseDC(_hwnd, hdcWindow);
        }
    }

    public float GetDpi() => _deviceDpi;

    public void ApplyQualitySettings(InterpolationMode interpolation, SmoothingMode smoothing,
        PixelOffsetMode pixelOffset, CompositingQuality compositing)
    {
        if (_hdcMem == 0) return;

        int mode = interpolation switch
        {
            InterpolationMode.Bilinear or
            InterpolationMode.HighQualityBilinear or
            InterpolationMode.Bicubic or
            InterpolationMode.HighQualityBicubic => Gdi32.HALFTONE,
            _ => Gdi32.COLORONCOLOR,
        };

        if (mode != _stretchMode)
        {
            _stretchMode = mode;
            Gdi32.SetStretchBltMode(_hdcMem, mode);
            if (mode == Gdi32.HALFTONE)
                Gdi32.SetBrushOrgEx(_hdcMem, 0, 0, 0);
        }

        _smoothingMode = smoothing;
        _compositingQuality = compositing;

        uint fontQuality = ResolveFontQuality(smoothing, compositing);
        if (fontQuality != _fontQuality)
        {
            _fontQuality = fontQuality;
            DisposeFontCache();
        }

        (_pixelOffsetX, _pixelOffsetY) = pixelOffset switch
        {
            PixelOffsetMode.Half => (0.5f, 0.5f),
            _ => (0f, 0f),
        };
    }

    public unsafe Vector2 MeasureString(string text, FontSpec font, float maxWidth)
        => MeasureString(text, font, maxWidth, TextFormatFlags.None);

    public Vector2 MeasureString(string text, FontSpec font, float maxWidth, TextFormatFlags flags)
    {
        if (_hdcMem == 0) return Vector2.Zero;
        if (string.IsNullOrEmpty(text)) return Vector2.Zero;

        nint hFont = GetOrCreateGdiFont(font);
        nint hOldFont = Gdi32.SelectObject(_hdcMem, hFont);
        try
        {
            bool finiteWidth = !float.IsPositiveInfinity(maxWidth);
            bool wrap = finiteWidth && (flags & TextFormatFlags.NoWrap) == 0;
            if (wrap || finiteWidth)
            {
                int layoutWidth = Math.Max(1, (int)MathF.Ceiling(maxWidth));
                RECT rc = new()
                {
                    left = 0,
                    top = 0,
                    right = layoutWidth,
                    bottom = 1_000_000,
                };

                uint format = BuildDrawTextFormat(TextAlignment.Near, flags, finiteWidth) | User32.DT_CALCRECT;
                User32.DrawText(_hdcMem, text, text.Length, ref rc, format);
                return new Vector2(Math.Max(0, rc.right - rc.left), Math.Max(0, rc.bottom - rc.top));
            }

            unsafe
            {
                fixed (char* pText = text)
                {
                    Gdi32.GetTextExtentPoint32Raw(_hdcMem, pText, text.Length, out SIZE size);
                    return new Vector2(size.cx, size.cy);
                }
            }
        }
        finally
        {
            Gdi32.SelectObject(_hdcMem, hOldFont);
        }
    }

    // --- Command execution ---

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
                    _savedTransforms.Push(_currentTransform);
                    Gdi32.SaveDC(_hdcMem);
                    break;
                case DrawCommandKind.Restore:
                    _currentTransform = _savedTransforms.Count > 0
                        ? _savedTransforms.Pop()
                        : Matrix3x2.Identity;
                    Gdi32.RestoreDC(_hdcMem, -1);
                    break;
                case DrawCommandKind.SetClip:
                    ExecuteSetClip(in cmd);
                    break;
                case DrawCommandKind.ResetClip:
                    Gdi32.SelectClipRgn(_hdcMem, 0);
                    ApplyViewportClip();
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
        int left = _isExternalHdc ? _viewportX : 0;
        int top = _isExternalHdc ? _viewportY : 0;
        RECT rc = new() { left = left, top = top, right = left + _width, bottom = top + _height };
        nint hBrush = Gdi32.CreateSolidBrush(ToColorRef(color));
        User32.FillRect(_hdcMem, ref rc, hBrush);
        Gdi32.DeleteObject(hBrush);
    }

    private void ExecuteDrawLine(in DrawCommand cmd)
    {
        if (_smoothingMode != SmoothingMode.None &&
            cmd.DashStyle == DashStyle.Solid &&
            TryRasterizeLineAA(in cmd))
        {
            return;
        }

        if (cmd.StrokeRenderMode == StrokeRenderMode.AlphaAccurate &&
            cmd.DashStyle == DashStyle.Solid &&
            cmd.Color.A > 0f &&
            cmd.Color.A < 0.999f &&
            TryRasterizeLineAlpha(in cmd))
        {
            return;
        }

        nint hPen = Gdi32.CreatePen(ToGdiPenStyle(cmd.DashStyle), (int)cmd.StrokeWidth, ToColorRef(cmd.Color));
        nint hOld = Gdi32.SelectObject(_hdcMem, hPen);
        Gdi32.MoveToEx(_hdcMem, (int)cmd.P1.X, (int)cmd.P1.Y, 0);
        Gdi32.LineTo(_hdcMem, (int)cmd.P2.X, (int)cmd.P2.Y);
        Gdi32.SelectObject(_hdcMem, hOld);
        Gdi32.DeleteObject(hPen);
    }

    private unsafe bool TryRasterizeLineAlpha(in DrawCommand cmd)
    {
        if (_bitmapBits == 0 || _width <= 0 || _height <= 0)
        {
            return false;
        }

        Vector2 a = Vector2.Transform(new Vector2(cmd.P1.X, cmd.P1.Y), _currentTransform);
        Vector2 b = Vector2.Transform(new Vector2(cmd.P2.X, cmd.P2.Y), _currentTransform);
        Vector2 ab = b - a;
        float lenSq = ab.LengthSquared();
        if (lenSq <= 0.0001f)
        {
            return false;
        }

        float halfWidth = MathF.Max(0.5f, cmd.StrokeWidth * 0.5f);
        float pad = halfWidth + 1f;
        float minXf = MathF.Min(a.X, b.X) - pad;
        float maxXf = MathF.Max(a.X, b.X) + pad;
        float minYf = MathF.Min(a.Y, b.Y) - pad;
        float maxYf = MathF.Max(a.Y, b.Y) + pad;

        int minX = Math.Max(0, (int)MathF.Floor(minXf));
        int maxX = Math.Min(_width - 1, (int)MathF.Ceiling(maxXf));
        int minY = Math.Max(0, (int)MathF.Floor(minYf));
        int maxY = Math.Min(_height - 1, (int)MathF.Ceiling(maxYf));
        if (minX > maxX || minY > maxY)
        {
            return true;
        }

        float srcR = Math.Clamp(cmd.Color.R, 0f, 1f);
        float srcG = Math.Clamp(cmd.Color.G, 0f, 1f);
        float srcB = Math.Clamp(cmd.Color.B, 0f, 1f);
        float srcA = Math.Clamp(cmd.Color.A, 0f, 1f);
        byte* pixels = (byte*)_bitmapBits;
        int stride = _width * 4;

        for (int y = minY; y <= maxY; y++)
        {
            float py = y + 0.5f;
            byte* row = pixels + (y * stride);
            for (int x = minX; x <= maxX; x++)
            {
                float px = x + 0.5f;
                Vector2 p = new(px, py);
                float t = Vector2.Dot(p - a, ab) / lenSq;
                t = Math.Clamp(t, 0f, 1f);
                Vector2 closest = a + (ab * t);
                float dist = Vector2.Distance(p, closest);
                float coverage = Math.Clamp((halfWidth + 0.5f - dist), 0f, 1f);
                if (coverage <= 0f)
                {
                    continue;
                }

                BlendPixel(row + (x * 4), srcR, srcG, srcB, srcA * coverage);
            }
        }

        return true;
    }

    private bool TryRasterizeLineAA(in DrawCommand cmd)
    {
        if (_bitmapBits == 0 || _width <= 0 || _height <= 0)
        {
            return false;
        }

        if (cmd.Color.A <= 0f || cmd.StrokeWidth <= 0f)
        {
            return true;
        }

        Vector2 a = Vector2.Transform(new Vector2(cmd.P1.X, cmd.P1.Y), _currentTransform);
        Vector2 b = Vector2.Transform(new Vector2(cmd.P2.X, cmd.P2.Y), _currentTransform);
        return RasterizeLineSegmentAA(a, b, cmd.StrokeWidth, cmd.Color);
    }

    private void ExecuteDrawRectangle(in DrawCommand cmd)
    {
        if (_smoothingMode != SmoothingMode.None &&
            cmd.DashStyle == DashStyle.Solid &&
            TryRasterizeRectangleStrokeAA(in cmd))
        {
            return;
        }

        nint hPen = Gdi32.CreatePen(ToGdiPenStyle(cmd.DashStyle), (int)cmd.StrokeWidth, ToColorRef(cmd.Color));
        nint hNullBrush = Gdi32.GetStockObject(Gdi32.NULL_BRUSH);
        nint hOldPen = Gdi32.SelectObject(_hdcMem, hPen);
        nint hOldBrush = Gdi32.SelectObject(_hdcMem, hNullBrush);

        Gdi32.Rectangle(_hdcMem,
            (int)cmd.Rect.X, (int)cmd.Rect.Y,
            (int)cmd.Rect.Right, (int)cmd.Rect.Bottom);

        Gdi32.SelectObject(_hdcMem, hOldBrush);
        Gdi32.SelectObject(_hdcMem, hOldPen);
        Gdi32.DeleteObject(hPen);
    }

    private bool TryRasterizeRectangleStrokeAA(in DrawCommand cmd)
    {
        if (_bitmapBits == 0 || _width <= 0 || _height <= 0)
        {
            return false;
        }

        if (cmd.Color.A <= 0f || cmd.StrokeWidth <= 0f)
        {
            return true;
        }

        Vector2 p0 = Vector2.Transform(new Vector2(cmd.Rect.X, cmd.Rect.Y), _currentTransform);
        Vector2 p1 = Vector2.Transform(new Vector2(cmd.Rect.Right, cmd.Rect.Y), _currentTransform);
        Vector2 p2 = Vector2.Transform(new Vector2(cmd.Rect.Right, cmd.Rect.Bottom), _currentTransform);
        Vector2 p3 = Vector2.Transform(new Vector2(cmd.Rect.X, cmd.Rect.Bottom), _currentTransform);

        bool any = false;
        any |= RasterizeLineSegmentAA(p0, p1, cmd.StrokeWidth, cmd.Color);
        any |= RasterizeLineSegmentAA(p1, p2, cmd.StrokeWidth, cmd.Color);
        any |= RasterizeLineSegmentAA(p2, p3, cmd.StrokeWidth, cmd.Color);
        any |= RasterizeLineSegmentAA(p3, p0, cmd.StrokeWidth, cmd.Color);
        return any;
    }

    private void ExecuteFillRectangle(in DrawCommand cmd)
    {
        if (cmd.Color.A <= 0f)
        {
            return;
        }

        if (cmd.Color.A < 0.999f && TryRasterizeRectAlpha(in cmd))
        {
            return;
        }

        RECT rc = new()
        {
            left = (int)cmd.Rect.X,
            top = (int)cmd.Rect.Y,
            right = (int)cmd.Rect.Right,
            bottom = (int)cmd.Rect.Bottom,
        };
        nint hBrush = Gdi32.CreateSolidBrush(ToColorRef(cmd.Color));
        User32.FillRect(_hdcMem, ref rc, hBrush);
        Gdi32.DeleteObject(hBrush);
    }

    private void ExecuteDrawEllipse(in DrawCommand cmd)
    {
        if (_smoothingMode != SmoothingMode.None && TryRasterizeEllipseAA(in cmd, fill: false))
        {
            return;
        }

        nint hPen = Gdi32.CreatePen(ToGdiPenStyle(cmd.DashStyle), (int)cmd.StrokeWidth, ToColorRef(cmd.Color));
        nint hNullBrush = Gdi32.GetStockObject(Gdi32.NULL_BRUSH);
        nint hOldPen = Gdi32.SelectObject(_hdcMem, hPen);
        nint hOldBrush = Gdi32.SelectObject(_hdcMem, hNullBrush);

        Gdi32.Ellipse(_hdcMem,
            (int)cmd.Rect.X, (int)cmd.Rect.Y,
            (int)cmd.Rect.Right, (int)cmd.Rect.Bottom);

        Gdi32.SelectObject(_hdcMem, hOldBrush);
        Gdi32.SelectObject(_hdcMem, hOldPen);
        Gdi32.DeleteObject(hPen);
    }

    private void ExecuteFillEllipse(in DrawCommand cmd)
    {
        if (_smoothingMode != SmoothingMode.None && TryRasterizeEllipseAA(in cmd, fill: true))
        {
            return;
        }

        nint hBrush = Gdi32.CreateSolidBrush(ToColorRef(cmd.Color));
        nint hNullPen = Gdi32.GetStockObject(Gdi32.NULL_PEN);
        nint hOldBrush = Gdi32.SelectObject(_hdcMem, hBrush);
        nint hOldPen = Gdi32.SelectObject(_hdcMem, hNullPen);

        Gdi32.Ellipse(_hdcMem,
            (int)cmd.Rect.X, (int)cmd.Rect.Y,
            (int)cmd.Rect.Right, (int)cmd.Rect.Bottom);

        Gdi32.SelectObject(_hdcMem, hOldPen);
        Gdi32.SelectObject(_hdcMem, hOldBrush);
        Gdi32.DeleteObject(hBrush);
    }

    private unsafe bool TryRasterizeEllipseAA(in DrawCommand cmd, bool fill)
    {
        if (_bitmapBits == 0 || _width <= 0 || _height <= 0)
        {
            return false;
        }

        var rect = cmd.Rect;
        var scaleX = MathF.Abs(_currentTransform.M11);
        var scaleY = MathF.Abs(_currentTransform.M22);
        if (scaleX <= 0.0001f)
        {
            scaleX = 1f;
        }

        if (scaleY <= 0.0001f)
        {
            scaleY = 1f;
        }

        rect = new RectF(
            (rect.X * scaleX) + _currentTransform.M31,
            (rect.Y * scaleY) + _currentTransform.M32,
            rect.Width * scaleX,
            rect.Height * scaleY);
        var rx = rect.Width * 0.5f;
        var ry = rect.Height * 0.5f;
        if (rx <= 0.01f || ry <= 0.01f)
        {
            return false;
        }

        var cx = rect.X + rx;
        var cy = rect.Y + ry;
        var minR = MathF.Max(1f, MathF.Min(rx, ry));
        var strokeScale = MathF.Max(1f, (scaleX + scaleY) * 0.5f);
        var strokeHalf = MathF.Max(0.5f, (cmd.StrokeWidth * strokeScale) * 0.5f);
        var aaPixels = 1.0f;

        var minX = Math.Max(0, (int)MathF.Floor(rect.X - strokeHalf - 2f));
        var minY = Math.Max(0, (int)MathF.Floor(rect.Y - strokeHalf - 2f));
        var maxX = Math.Min(_width - 1, (int)MathF.Ceiling(rect.Right + strokeHalf + 2f));
        var maxY = Math.Min(_height - 1, (int)MathF.Ceiling(rect.Bottom + strokeHalf + 2f));
        if (minX > maxX || minY > maxY)
        {
            return true;
        }

        var srcR = Math.Clamp(cmd.Color.R, 0f, 1f);
        var srcG = Math.Clamp(cmd.Color.G, 0f, 1f);
        var srcB = Math.Clamp(cmd.Color.B, 0f, 1f);
        var srcA = Math.Clamp(cmd.Color.A, 0f, 1f);
        if (srcA <= 0f)
        {
            return true;
        }

        var pixels = (byte*)_bitmapBits;
        var stride = _width * 4;

        for (var y = minY; y <= maxY; y++)
        {
            var py = y + 0.5f;
            var dy = (py - cy) / ry;
            var dySq = dy * dy;
            if (dySq > 4f)
            {
                continue;
            }

            var row = pixels + (y * stride);
            for (var x = minX; x <= maxX; x++)
            {
                var px = x + 0.5f;
                var dx = (px - cx) / rx;
                var dNorm = MathF.Sqrt((dx * dx) + dySq);
                var signedDistPx = (dNorm - 1f) * minR;
                float coverage;
                if (fill)
                {
                    coverage = Math.Clamp(0.5f - signedDistPx / aaPixels, 0f, 1f);
                }
                else
                {
                    coverage = Math.Clamp((strokeHalf + 0.5f - MathF.Abs(signedDistPx)) / aaPixels, 0f, 1f);
                }

                if (coverage <= 0f)
                {
                    continue;
                }

                var a = srcA * coverage;
                var invA = 1f - a;
                var p = row + (x * 4);
                var dstB = p[0] / 255f;
                var dstG = p[1] / 255f;
                var dstR = p[2] / 255f;
                p[0] = (byte)((((srcB * a) + (dstB * invA)) * 255f) + 0.5f);
                p[1] = (byte)((((srcG * a) + (dstG * invA)) * 255f) + 0.5f);
                p[2] = (byte)((((srcR * a) + (dstR * invA)) * 255f) + 0.5f);
                p[3] = 255;
            }
        }

        return true;
    }

    private unsafe void ExecuteDrawString(in DrawCommand cmd)
    {
        if (cmd.Text is null) return;
        if (cmd.Text.Length == 0) return;

        nint hFont = GetOrCreateGdiFont(cmd.Font);
        nint hOldFont = Gdi32.SelectObject(_hdcMem, hFont);
        Gdi32.SetBkMode(_hdcMem, Gdi32.TRANSPARENT);
        Gdi32.SetTextColor(_hdcMem, ToColorRef(cmd.Color));

        if (cmd.Text.IndexOfAny(['\r', '\n']) >= 0 && cmd.GlyphAdvances is null && cmd.GlyphUniformAdvance <= 0)
        {
            // GDI TextOut/ExtTextOut do not handle multi-line text, so split and render line-by-line.
            Gdi32.GetTextMetrics(_hdcMem, out TEXTMETRIC tm);
            int lineAdvance = Math.Max(1, tm.tmHeight);
            int lineIndex = 0;
            int start = 0;
            while (start <= cmd.Text.Length)
            {
                int lineEnd = start;
                while (lineEnd < cmd.Text.Length && cmd.Text[lineEnd] != '\r' && cmd.Text[lineEnd] != '\n')
                    lineEnd++;

                string line = cmd.Text[start..lineEnd];
                int xLine = (int)cmd.P1.X;
                int yLine = (int)cmd.P1.Y + (lineIndex * lineAdvance);
                bool finiteWidth = !float.IsPositiveInfinity(cmd.MaxWidth);

                if (finiteWidth)
                {
                    int layoutWidth = Math.Max(1, (int)MathF.Ceiling(cmd.MaxWidth));
                    RECT layout = new()
                    {
                        left = xLine,
                        top = yLine,
                        right = xLine + layoutWidth,
                        bottom = yLine + lineAdvance + 8,
                    };
                    uint format = BuildDrawTextFormat(cmd.TextAlignment, cmd.TextFlags | TextFormatFlags.NoWrap, true) | User32.DT_SINGLELINE;
                    User32.DrawText(_hdcMem, line, line.Length, ref layout, format);
                }
                else
                {
                    if (cmd.TextAlignment != TextAlignment.Near)
                    {
                        unsafe
                        {
                            fixed (char* pMeasure = line)
                            {
                                Gdi32.GetTextExtentPoint32Raw(_hdcMem, pMeasure, line.Length, out SIZE textSize);
                                if (cmd.TextAlignment == TextAlignment.Center)
                                    xLine += (int)(-textSize.cx * 0.5f);
                                else
                                    xLine -= textSize.cx;
                            }
                        }
                    }

                    if (!IsIdentityTransform(_currentTransform) && TryDrawTextAsPath(xLine, yLine, line, cmd.Color))
                    {
                        // done
                    }
                    else
                    {
                        unsafe
                        {
                            fixed (char* pLine = line)
                            {
                                Gdi32.TextOutRaw(_hdcMem, xLine, yLine, pLine, line.Length);
                            }
                        }
                    }
                }

                if (lineEnd >= cmd.Text.Length)
                    break;

                if (cmd.Text[lineEnd] == '\r' && lineEnd + 1 < cmd.Text.Length && cmd.Text[lineEnd + 1] == '\n')
                    start = lineEnd + 2;
                else
                    start = lineEnd + 1;
                lineIndex++;
            }

            Gdi32.SelectObject(_hdcMem, hOldFont);
            return;
        }

        int x = (int)cmd.P1.X;
        int y = (int)cmd.P1.Y;
        int drawWidth = 0;
        if (cmd.GlyphAdvances is { Length: > 0 } advances)
        {
            int limit = Math.Min(advances.Length, cmd.Text.Length);
            for (int i = 0; i < limit; i++)
            {
                drawWidth += advances[i];
            }
        }
        else if (cmd.GlyphUniformAdvance > 0)
        {
            drawWidth = cmd.GlyphUniformAdvance * cmd.Text.Length;
        }

        if (cmd.TextRenderMode == TextRenderMode.AlphaAccurate)
        {
            if (TryDrawStringAlphaAccurate(in cmd, hFont, x, y, drawWidth))
            {
                Gdi32.SelectObject(_hdcMem, hOldFont);
                return;
            }
        }

        if (cmd.GlyphAdvances is { Length: > 0 } dx && dx.Length >= cmd.Text.Length)
        {
            AdjustXForAlignment(ref x, cmd, drawWidth);
            fixed (char* pText = cmd.Text)
            fixed (int* pDx = dx)
            {
                if (!Gdi32.ExtTextOutRaw(_hdcMem, x, y, 0u, 0, pText, (uint)cmd.Text.Length, pDx))
                {
                    Gdi32.TextOutRaw(_hdcMem, x, y, pText, cmd.Text.Length);
                }
            }
        }
        else if (cmd.GlyphUniformAdvance > 0)
        {
            AdjustXForAlignment(ref x, cmd, drawWidth);
            Span<int> localDx = cmd.Text.Length <= 1024
                ? stackalloc int[cmd.Text.Length]
                : new int[cmd.Text.Length];
            localDx.Fill(cmd.GlyphUniformAdvance);

            fixed (char* pText = cmd.Text)
            fixed (int* pDx = localDx)
            {
                if (!Gdi32.ExtTextOutRaw(_hdcMem, x, y, 0u, 0, pText, (uint)cmd.Text.Length, pDx))
                {
                    Gdi32.TextOutRaw(_hdcMem, x, y, pText, cmd.Text.Length);
                }
            }
        }
        else
        {
            bool finiteWidth = !float.IsPositiveInfinity(cmd.MaxWidth);
            if (finiteWidth)
            {
                // DrawText handles alignment via DT_CENTER/DT_RIGHT — use original x.
                int layoutWidth = Math.Max(1, (int)MathF.Ceiling(cmd.MaxWidth));
                RECT layout = new()
                {
                    left = x,
                    top = y,
                    right = x + layoutWidth,
                    bottom = y + 1_000_000,
                };
                uint format = BuildDrawTextFormat(cmd.TextAlignment, cmd.TextFlags, finiteWidth);
                User32.DrawText(_hdcMem, cmd.Text, cmd.Text.Length, ref layout, format);
            }
            else
            {
                AdjustXForAlignment(ref x, cmd, drawWidth);
                // For non-identity transforms, render glyph outlines as a path.
                // This aligns better with Direct2D transformed text behavior.
                if (!IsIdentityTransform(_currentTransform) && TryDrawTextAsPath(x, y, cmd.Text, cmd.Color))
                {
                    // done
                }
                else
                {
                    fixed (char* pText = cmd.Text)
                    {
                        Gdi32.TextOutRaw(_hdcMem, x, y, pText, cmd.Text.Length);
                    }
                }
            }
        }

        Gdi32.SelectObject(_hdcMem, hOldFont);
    }

    private unsafe bool TryDrawStringAlphaAccurate(in DrawCommand cmd, nint hFont, int x, int y, int drawWidth)
    {
        if (cmd.Text is null || cmd.Text.Length == 0)
            return true;
        if (cmd.Color.A >= 0.999f || cmd.Color.A <= 0f)
            return false;
        if (_bitmapBits == 0 || _width <= 0 || _height <= 0)
            return false;
        if (!IsIdentityTransform(_currentTransform))
            return false;
        if (cmd.Text.IndexOfAny(['\r', '\n']) >= 0)
            return false;
        if (cmd.GlyphAdvances is { Length: > 0 } || cmd.GlyphUniformAdvance > 0)
            return false;
        if (!float.IsPositiveInfinity(cmd.MaxWidth))
            return false;

        int xDraw = x;
        AdjustXForAlignment(ref xDraw, cmd, drawWidth);

        SIZE textSize;
        fixed (char* pMeasure = cmd.Text)
        {
            Gdi32.GetTextExtentPoint32Raw(_hdcMem, pMeasure, cmd.Text.Length, out textSize);
        }

        int pad = 2;
        int maskW = Math.Max(1, textSize.cx + (pad * 2));
        int maskH = Math.Max(1, textSize.cy + (pad * 2));
        int dstX = xDraw - pad;
        int dstY = y - pad;

        nint hdcTemp = Gdi32.CreateCompatibleDC(_hdcMem);
        if (hdcTemp == 0)
            return false;

        BITMAPINFO bmi = new();
        bmi.bmiHeader.biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>();
        bmi.bmiHeader.biWidth = maskW;
        bmi.bmiHeader.biHeight = -maskH; // top-down
        bmi.bmiHeader.biPlanes = 1;
        bmi.bmiHeader.biBitCount = 32;
        bmi.bmiHeader.biCompression = 0; // BI_RGB

        nint hBmp = 0;
        nint hOldBmp = 0;
        nint hOldFont = 0;
        try
        {
            hBmp = Gdi32.CreateDIBSection(hdcTemp, ref bmi, Gdi32.DIB_RGB_COLORS, out nint maskBits, 0, 0);
            if (hBmp == 0 || maskBits == 0)
                return false;

            hOldBmp = Gdi32.SelectObject(hdcTemp, hBmp);
            hOldFont = Gdi32.SelectObject(hdcTemp, hFont);

            Gdi32.SetBkMode(hdcTemp, Gdi32.TRANSPARENT);
            Gdi32.SetTextColor(hdcTemp, 0x00FFFFFF);

            fixed (char* pText = cmd.Text)
            {
                Gdi32.TextOutRaw(hdcTemp, pad, pad, pText, cmd.Text.Length);
            }

            float srcR = Math.Clamp(cmd.Color.R, 0f, 1f);
            float srcG = Math.Clamp(cmd.Color.G, 0f, 1f);
            float srcB = Math.Clamp(cmd.Color.B, 0f, 1f);
            float alphaScale = Math.Clamp(cmd.Color.A, 0f, 1f);

            byte* mask = (byte*)maskBits;
            byte* dst = (byte*)_bitmapBits;
            int dstStride = _width * 4;
            int maskStride = maskW * 4;

            for (int sy = 0; sy < maskH; sy++)
            {
                int dy = dstY + sy;
                if ((uint)dy >= (uint)_height)
                    continue;

                byte* srcRow = mask + (sy * maskStride);
                byte* dstRow = dst + (dy * dstStride);
                for (int sx = 0; sx < maskW; sx++)
                {
                    int dx = dstX + sx;
                    if ((uint)dx >= (uint)_width)
                        continue;

                    byte* sp = srcRow + (sx * 4);
                    float coverage = Math.Max(sp[0], Math.Max(sp[1], sp[2])) / 255f;
                    if (coverage <= 0f)
                        continue;

                    BlendPixel(dstRow + (dx * 4), srcR, srcG, srcB, alphaScale * coverage);
                }
            }

            return true;
        }
        finally
        {
            if (hOldFont != 0)
                Gdi32.SelectObject(hdcTemp, hOldFont);
            if (hOldBmp != 0)
                Gdi32.SelectObject(hdcTemp, hOldBmp);
            if (hBmp != 0)
                Gdi32.DeleteObject(hBmp);
            Gdi32.DeleteDC(hdcTemp);
        }
    }

    private void ExecuteSetTransform(in DrawCommand cmd)
    {
        _currentTransform = cmd.Transform;
        _currentTransform.M31 += _viewportX + _pixelOffsetX;
        _currentTransform.M32 += _viewportY + _pixelOffsetY;
        var xform = new XFORM
        {
            eM11 = cmd.Transform.M11,
            eM12 = cmd.Transform.M12,
            eM21 = cmd.Transform.M21,
            eM22 = cmd.Transform.M22,
            eDx = _currentTransform.M31,
            eDy = _currentTransform.M32,
        };
        Gdi32.SetWorldTransform(_hdcMem, ref xform);
    }

    private void ExecuteSetClip(in DrawCommand cmd)
    {
        // Match Direct2D semantics: clip rect is interpreted at the current
        // transform and then remains axis-aligned in device space.
        RectF deviceRect = TransformRect(cmd.Rect);
        int left = (int)MathF.Floor(deviceRect.X);
        int top = (int)MathF.Floor(deviceRect.Y);
        int right = (int)MathF.Ceiling(deviceRect.Right);
        int bottom = (int)MathF.Ceiling(deviceRect.Bottom);

        nint hrgn = Gdi32.CreateRectRgn(left, top, right, bottom);
        if (hrgn == 0)
        {
            return;
        }

        var identity = new XFORM { eM11 = 1f, eM22 = 1f };
        Gdi32.SetWorldTransform(_hdcMem, ref identity);
        Gdi32.ExtSelectClipRgn(_hdcMem, hrgn, Gdi32.RGN_AND);
        var currentXform = new XFORM
        {
            eM11 = _currentTransform.M11,
            eM12 = _currentTransform.M12,
            eM21 = _currentTransform.M21,
            eM22 = _currentTransform.M22,
            eDx = _currentTransform.M31,
            eDy = _currentTransform.M32,
        };
        Gdi32.SetWorldTransform(_hdcMem, ref currentXform);
        Gdi32.DeleteObject(hrgn);
    }

    private void ExecuteResetTransform()
    {
        _currentTransform = Matrix3x2.CreateTranslation(_viewportX + _pixelOffsetX, _viewportY + _pixelOffsetY);
        var xform = new XFORM
        {
            eM11 = _currentTransform.M11,
            eM12 = _currentTransform.M12,
            eM21 = _currentTransform.M21,
            eM22 = _currentTransform.M22,
            eDx = _currentTransform.M31,
            eDy = _currentTransform.M32,
        };
        Gdi32.SetWorldTransform(_hdcMem, ref xform);
    }

    private unsafe void ExecuteDrawImage(in DrawCommand cmd)
    {
        if (!ImageManager.TryGetImageData(cmd.Image, out var imgData)) return;

        nint hdcTemp = Gdi32.CreateCompatibleDC(_hdcMem);

        BITMAPINFO bmi = new();
        bmi.bmiHeader.biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>();
        bmi.bmiHeader.biWidth = imgData.Width;
        bmi.bmiHeader.biHeight = -imgData.Height; // top-down
        bmi.bmiHeader.biPlanes = 1;
        bmi.bmiHeader.biBitCount = 32;
        bmi.bmiHeader.biCompression = 0; // BI_RGB

        nint hBmp = Gdi32.CreateDIBSection(hdcTemp, ref bmi, Gdi32.DIB_RGB_COLORS, out nint bits, 0, 0);
        nint hOld = Gdi32.SelectObject(hdcTemp, hBmp);

        // Copy pixel data into the DIB
        fixed (byte* src = imgData.BgraPixels)
        {
            Buffer.MemoryCopy(src, (void*)bits, imgData.BgraPixels.Length, imgData.BgraPixels.Length);
        }

        // Determine source rect
        int srcX, srcY, srcW, srcH;
        if (cmd.HasSrcRect)
        {
            var sr = cmd.SrcRect;
            srcX = (int)sr.X;
            srcY = (int)sr.Y;
            srcW = (int)sr.Width;
            srcH = (int)sr.Height;
        }
        else
        {
            srcX = 0; srcY = 0;
            srcW = imgData.Width;
            srcH = imgData.Height;
        }

        int destX = (int)cmd.Rect.X;
        int destY = (int)cmd.Rect.Y;
        int destW = (int)cmd.Rect.Width;
        int destH = (int)cmd.Rect.Height;

        bool hasPerPixelAlpha = _compositingQuality == CompositingQuality.HighSpeed
            ? false
            : ShouldUsePerPixelAlpha(imgData, srcX, srcY, srcW, srcH);
        bool useAlphaBlend = cmd.Opacity < 1f || hasPerPixelAlpha;
        if (useAlphaBlend)
        {
            if (hasPerPixelAlpha)
            {
                PremultiplyAlphaInPlace((byte*)bits, imgData.Width, imgData.Height);
            }

            var blend = new BLENDFUNCTION
            {
                BlendOp = Msimg32.AC_SRC_OVER,
                BlendFlags = 0,
                SourceConstantAlpha = (byte)(cmd.Opacity * 255f + 0.5f),
                AlphaFormat = Msimg32.AC_SRC_ALPHA,
            };
            Msimg32.AlphaBlend(_hdcMem, destX, destY, destW, destH,
                hdcTemp, srcX, srcY, srcW, srcH, blend);
        }
        else
        {
            Gdi32.StretchBlt(_hdcMem, destX, destY, destW, destH,
                hdcTemp, srcX, srcY, srcW, srcH, Gdi32.SRCCOPY);
        }

        Gdi32.SelectObject(hdcTemp, hOld);
        Gdi32.DeleteObject(hBmp);
        Gdi32.DeleteDC(hdcTemp);
    }

    private static bool ShouldUsePerPixelAlpha(in ImageData imgData, int srcX, int srcY, int srcW, int srcH)
    {
        int x0 = Math.Clamp(srcX, 0, imgData.Width - 1);
        int y0 = Math.Clamp(srcY, 0, imgData.Height - 1);
        int x1 = Math.Clamp(srcX + Math.Max(1, srcW), 0, imgData.Width);
        int y1 = Math.Clamp(srcY + Math.Max(1, srcH), 0, imgData.Height);
        int stride = imgData.Stride;
        byte[] px = imgData.BgraPixels;
        int alphaZero = 0;
        int alphaFull = 0;
        int alphaMid = 0;
        int zeroAlphaWithColor = 0;
        int zeroAlphaBlack = 0;

        for (int y = y0; y < y1; y++)
        {
            int row = y * stride;
            for (int x = x0; x < x1; x++)
            {
                int i = row + x * 4;
                byte b = px[i + 0];
                byte g = px[i + 1];
                byte r = px[i + 2];
                byte a = px[row + x * 4 + 3];
                if (a == 0)
                {
                    alphaZero++;
                    if ((r | g | b) == 0) zeroAlphaBlack++;
                    else zeroAlphaWithColor++;
                }
                else if (a == 255)
                {
                    alphaFull++;
                }
                else
                {
                    alphaMid++;
                }
            }
        }

        // True alpha edges/transitions: definitely use per-pixel alpha.
        if (alphaMid > 0)
            return true;

        // No transparent pixels.
        if (alphaZero == 0)
            return false;

        // Fully transparent + fully opaque with mostly black transparent pixels:
        // likely intentional cutout transparency (e.g., circular sprite).
        if (alphaFull > 0 && zeroAlphaBlack >= zeroAlphaWithColor * 4)
            return true;

        // Otherwise treat alpha as unreliable metadata (common for captured buffers).
        return false;
    }

    private static unsafe void PremultiplyAlphaInPlace(byte* bgra, int width, int height)
    {
        int stride = width * 4;
        for (int y = 0; y < height; y++)
        {
            byte* row = bgra + y * stride;
            for (int x = 0; x < width; x++)
            {
                byte* p = row + x * 4;
                byte a = p[3];
                if (a == 255) continue;
                if (a == 0)
                {
                    p[0] = 0;
                    p[1] = 0;
                    p[2] = 0;
                    continue;
                }

                p[0] = (byte)((p[0] * a + 127) / 255);
                p[1] = (byte)((p[1] * a + 127) / 255);
                p[2] = (byte)((p[2] * a + 127) / 255);
            }
        }
    }

    private void ExecuteDrawRoundedRectangle(in DrawCommand cmd)
    {
        int ellipseSize = (int)(cmd.CornerRadius * 2);
        nint hPen = Gdi32.CreatePen(ToGdiPenStyle(cmd.DashStyle), (int)cmd.StrokeWidth, ToColorRef(cmd.Color));
        nint hNullBrush = Gdi32.GetStockObject(Gdi32.NULL_BRUSH);
        nint hOldPen = Gdi32.SelectObject(_hdcMem, hPen);
        nint hOldBrush = Gdi32.SelectObject(_hdcMem, hNullBrush);

        Gdi32.RoundRect(_hdcMem,
            (int)cmd.Rect.X, (int)cmd.Rect.Y,
            (int)cmd.Rect.Right, (int)cmd.Rect.Bottom,
            ellipseSize, ellipseSize);

        Gdi32.SelectObject(_hdcMem, hOldBrush);
        Gdi32.SelectObject(_hdcMem, hOldPen);
        Gdi32.DeleteObject(hPen);
    }

    private void ExecuteFillRoundedRectangle(in DrawCommand cmd)
    {
        if (cmd.Color.A <= 0f)
        {
            return;
        }

        if (cmd.Color.A < 0.999f && TryRasterizeRoundedRectAlpha(in cmd))
        {
            return;
        }

        int ellipseSize = (int)(cmd.CornerRadius * 2);
        nint hBrush = Gdi32.CreateSolidBrush(ToColorRef(cmd.Color));
        nint hNullPen = Gdi32.GetStockObject(Gdi32.NULL_PEN);
        nint hOldBrush = Gdi32.SelectObject(_hdcMem, hBrush);
        nint hOldPen = Gdi32.SelectObject(_hdcMem, hNullPen);

        Gdi32.RoundRect(_hdcMem,
            (int)cmd.Rect.X, (int)cmd.Rect.Y,
            (int)cmd.Rect.Right, (int)cmd.Rect.Bottom,
            ellipseSize, ellipseSize);

        Gdi32.SelectObject(_hdcMem, hOldPen);
        Gdi32.SelectObject(_hdcMem, hOldBrush);
        Gdi32.DeleteObject(hBrush);
    }

    private unsafe bool TryRasterizeRectAlpha(in DrawCommand cmd)
    {
        if (_bitmapBits == 0 || _width <= 0 || _height <= 0)
        {
            return false;
        }

        RectF rect = TransformRect(cmd.Rect);
        if (!TryGetRasterBounds(rect, out int minX, out int minY, out int maxX, out int maxY))
        {
            return true;
        }

        if (!Matrix3x2.Invert(_currentTransform, out Matrix3x2 inverseTransform))
        {
            return false;
        }

        var srcR = Math.Clamp(cmd.Color.R, 0f, 1f);
        var srcG = Math.Clamp(cmd.Color.G, 0f, 1f);
        var srcB = Math.Clamp(cmd.Color.B, 0f, 1f);
        var srcA = Math.Clamp(cmd.Color.A, 0f, 1f);
        if (srcA <= 0f)
        {
            return true;
        }

        float left = cmd.Rect.X;
        float top = cmd.Rect.Y;
        float right = cmd.Rect.Right;
        float bottom = cmd.Rect.Bottom;
        var pixels = (byte*)_bitmapBits;
        var stride = _width * 4;
        var identity = new XFORM { eM11 = 1f, eM22 = 1f };
        Gdi32.SetWorldTransform(_hdcMem, ref identity);

        for (int y = minY; y <= maxY; y++)
        {
            var row = pixels + (y * stride);
            for (int x = minX; x <= maxX; x++)
            {
                if (!Gdi32.PtVisible(_hdcMem, x, y))
                {
                    continue;
                }

                var local = Vector2.Transform(new Vector2(x + 0.5f, y + 0.5f), inverseTransform);
                if (local.X < left || local.X >= right || local.Y < top || local.Y >= bottom)
                {
                    continue;
                }

                BlendPixel(row + (x * 4), srcR, srcG, srcB, srcA);
            }
        }
        var currentXform = new XFORM
        {
            eM11 = _currentTransform.M11,
            eM12 = _currentTransform.M12,
            eM21 = _currentTransform.M21,
            eM22 = _currentTransform.M22,
            eDx = _currentTransform.M31,
            eDy = _currentTransform.M32,
        };
        Gdi32.SetWorldTransform(_hdcMem, ref currentXform);

        return true;
    }

    private void ApplyViewportClip()
    {
        if (_isExternalHdc && (_viewportX != 0 || _viewportY != 0))
        {
            Gdi32.IntersectClipRect(_hdcMem, _viewportX, _viewportY, _viewportX + _width, _viewportY + _height);
        }
    }

    private unsafe bool TryRasterizeRoundedRectAlpha(in DrawCommand cmd)
    {
        if (_bitmapBits == 0 || _width <= 0 || _height <= 0)
        {
            return false;
        }

        RectF rect = TransformRect(cmd.Rect);
        if (!TryGetRasterBounds(rect, out int minX, out int minY, out int maxX, out int maxY))
        {
            return true;
        }

        if (!Matrix3x2.Invert(_currentTransform, out Matrix3x2 inverseTransform))
        {
            return false;
        }

        var srcR = Math.Clamp(cmd.Color.R, 0f, 1f);
        var srcG = Math.Clamp(cmd.Color.G, 0f, 1f);
        var srcB = Math.Clamp(cmd.Color.B, 0f, 1f);
        var srcA = Math.Clamp(cmd.Color.A, 0f, 1f);
        if (srcA <= 0f)
        {
            return true;
        }

        var pixels = (byte*)_bitmapBits;
        var stride = _width * 4;
        float left = cmd.Rect.X;
        float top = cmd.Rect.Y;
        float right = cmd.Rect.Right;
        float bottom = cmd.Rect.Bottom;
        float radius = MathF.Max(0f, cmd.CornerRadius);
        radius = MathF.Min(radius, MathF.Min(cmd.Rect.Width, cmd.Rect.Height) * 0.5f);
        var identity = new XFORM { eM11 = 1f, eM22 = 1f };
        Gdi32.SetWorldTransform(_hdcMem, ref identity);

        for (int y = minY; y <= maxY; y++)
        {
            var row = pixels + (y * stride);
            for (int x = minX; x <= maxX; x++)
            {
                if (!Gdi32.PtVisible(_hdcMem, x, y))
                {
                    continue;
                }

                var local = Vector2.Transform(new Vector2(x + 0.5f, y + 0.5f), inverseTransform);
                if (!ContainsRoundedRectPoint(local.X, local.Y, left, top, right, bottom, radius))
                {
                    continue;
                }

                BlendPixel(row + (x * 4), srcR, srcG, srcB, srcA);
            }
        }
        var currentXform = new XFORM
        {
            eM11 = _currentTransform.M11,
            eM12 = _currentTransform.M12,
            eM21 = _currentTransform.M21,
            eM22 = _currentTransform.M22,
            eDx = _currentTransform.M31,
            eDy = _currentTransform.M32,
        };
        Gdi32.SetWorldTransform(_hdcMem, ref currentXform);

        return true;
    }

    private static bool ContainsRoundedRectPoint(float px, float py, float left, float top, float right, float bottom, float radius)
    {
        if (px < left || px >= right || py < top || py >= bottom)
        {
            return false;
        }

        if (radius <= 0.01f)
        {
            return true;
        }

        float innerLeft = left + radius;
        float innerRight = right - radius;
        float innerTop = top + radius;
        float innerBottom = bottom - radius;

        if ((px >= innerLeft && px < innerRight) || (py >= innerTop && py < innerBottom))
        {
            return true;
        }

        float cx = px < innerLeft ? innerLeft : innerRight;
        float cy = py < innerTop ? innerTop : innerBottom;
        float dx = px - cx;
        float dy = py - cy;
        return (dx * dx) + (dy * dy) <= radius * radius;
    }

    private unsafe void BlendRectPixels(int minX, int minY, int maxX, int maxY, ColorF color)
    {
        var srcR = Math.Clamp(color.R, 0f, 1f);
        var srcG = Math.Clamp(color.G, 0f, 1f);
        var srcB = Math.Clamp(color.B, 0f, 1f);
        var srcA = Math.Clamp(color.A, 0f, 1f);
        if (srcA <= 0f)
        {
            return;
        }

        var pixels = (byte*)_bitmapBits;
        var stride = _width * 4;
        var invA = 1f - srcA;
        for (int y = minY; y <= maxY; y++)
        {
            var row = pixels + (y * stride);
            for (int x = minX; x <= maxX; x++)
            {
                var p = row + (x * 4);
                var dstB = p[0] / 255f;
                var dstG = p[1] / 255f;
                var dstR = p[2] / 255f;
                p[0] = (byte)((((srcB * srcA) + (dstB * invA)) * 255f) + 0.5f);
                p[1] = (byte)((((srcG * srcA) + (dstG * invA)) * 255f) + 0.5f);
                p[2] = (byte)((((srcR * srcA) + (dstR * invA)) * 255f) + 0.5f);
                p[3] = 255;
            }
        }
    }

    private RectF TransformRect(RectF rect)
    {
        var p0 = Vector2.Transform(new Vector2(rect.X, rect.Y), _currentTransform);
        var p1 = Vector2.Transform(new Vector2(rect.Right, rect.Y), _currentTransform);
        var p2 = Vector2.Transform(new Vector2(rect.X, rect.Bottom), _currentTransform);
        var p3 = Vector2.Transform(new Vector2(rect.Right, rect.Bottom), _currentTransform);

        float minX = MathF.Min(MathF.Min(p0.X, p1.X), MathF.Min(p2.X, p3.X));
        float minY = MathF.Min(MathF.Min(p0.Y, p1.Y), MathF.Min(p2.Y, p3.Y));
        float maxX = MathF.Max(MathF.Max(p0.X, p1.X), MathF.Max(p2.X, p3.X));
        float maxY = MathF.Max(MathF.Max(p0.Y, p1.Y), MathF.Max(p2.Y, p3.Y));

        return new RectF(minX, minY, maxX - minX, maxY - minY);
    }

    private bool TryGetRasterBounds(RectF bounds, out int minX, out int minY, out int maxX, out int maxY)
    {
        minX = Math.Max(0, (int)MathF.Floor(bounds.X));
        minY = Math.Max(0, (int)MathF.Floor(bounds.Y));
        maxX = Math.Min(_width - 1, (int)MathF.Ceiling(bounds.Right) - 1);
        maxY = Math.Min(_height - 1, (int)MathF.Ceiling(bounds.Bottom) - 1);

        RECT clipRect;
        int clipResult = Gdi32.GetClipBox(_hdcMem, out clipRect);
        if (clipResult == Gdi32.NULLREGION)
        {
            return false;
        }

        if (clipResult != Gdi32.ERROR)
        {
            minX = Math.Max(minX, clipRect.left);
            minY = Math.Max(minY, clipRect.top);
            maxX = Math.Min(maxX, clipRect.right - 1);
            maxY = Math.Min(maxY, clipRect.bottom - 1);
        }

        return minX <= maxX && minY <= maxY;
    }

    private static unsafe void BlendPixel(byte* p, float srcR, float srcG, float srcB, float srcA)
    {
        float invA = 1f - srcA;
        float dstB = p[0] / 255f;
        float dstG = p[1] / 255f;
        float dstR = p[2] / 255f;
        p[0] = (byte)((((srcB * srcA) + (dstB * invA)) * 255f) + 0.5f);
        p[1] = (byte)((((srcG * srcA) + (dstG * invA)) * 255f) + 0.5f);
        p[2] = (byte)((((srcR * srcA) + (dstR * invA)) * 255f) + 0.5f);
        p[3] = 255;
    }

    private unsafe bool RasterizeLineSegmentAA(Vector2 a, Vector2 b, float strokeWidth, ColorF color)
    {
        Vector2 ab = b - a;
        float lenSq = ab.LengthSquared();
        if (lenSq <= 0.0001f)
        {
            return false;
        }

        float basisX = MathF.Sqrt((_currentTransform.M11 * _currentTransform.M11) + (_currentTransform.M12 * _currentTransform.M12));
        float basisY = MathF.Sqrt((_currentTransform.M21 * _currentTransform.M21) + (_currentTransform.M22 * _currentTransform.M22));
        float strokeScale = MathF.Max(1f, (basisX + basisY) * 0.5f);
        float halfWidth = MathF.Max(0.5f, strokeWidth * 0.5f * strokeScale);
        float aaPixels = 1f;
        float pad = halfWidth + aaPixels + 1f;

        int minX = Math.Max(0, (int)MathF.Floor(MathF.Min(a.X, b.X) - pad));
        int maxX = Math.Min(_width - 1, (int)MathF.Ceiling(MathF.Max(a.X, b.X) + pad));
        int minY = Math.Max(0, (int)MathF.Floor(MathF.Min(a.Y, b.Y) - pad));
        int maxY = Math.Min(_height - 1, (int)MathF.Ceiling(MathF.Max(a.Y, b.Y) + pad));
        if (minX > maxX || minY > maxY)
        {
            return true;
        }

        float srcR = Math.Clamp(color.R, 0f, 1f);
        float srcG = Math.Clamp(color.G, 0f, 1f);
        float srcB = Math.Clamp(color.B, 0f, 1f);
        float srcA = Math.Clamp(color.A, 0f, 1f);
        if (srcA <= 0f)
        {
            return true;
        }

        byte* pixels = (byte*)_bitmapBits;
        int stride = _width * 4;
        var identity = new XFORM { eM11 = 1f, eM22 = 1f };
        Gdi32.SetWorldTransform(_hdcMem, ref identity);

        for (int y = minY; y <= maxY; y++)
        {
            float py = y + 0.5f;
            byte* row = pixels + (y * stride);
            for (int x = minX; x <= maxX; x++)
            {
                if (!Gdi32.PtVisible(_hdcMem, x, y))
                {
                    continue;
                }

                float px = x + 0.5f;
                Vector2 p = new(px, py);
                float t = Vector2.Dot(p - a, ab) / lenSq;
                t = Math.Clamp(t, 0f, 1f);
                Vector2 closest = a + (ab * t);
                float dist = Vector2.Distance(p, closest);
                float coverage = Math.Clamp((halfWidth + 0.5f - dist) / aaPixels, 0f, 1f);
                if (coverage <= 0f)
                {
                    continue;
                }

                BlendPixel(row + (x * 4), srcR, srcG, srcB, srcA * coverage);
            }
        }

        var currentXform = new XFORM
        {
            eM11 = _currentTransform.M11,
            eM12 = _currentTransform.M12,
            eM21 = _currentTransform.M21,
            eM22 = _currentTransform.M22,
            eDx = _currentTransform.M31,
            eDy = _currentTransform.M32,
        };
        Gdi32.SetWorldTransform(_hdcMem, ref currentXform);
        return true;
    }

    private static uint BuildDrawTextFormat(TextAlignment alignment, TextFormatFlags flags, bool finiteWidth)
    {
        uint format = alignment switch
        {
            TextAlignment.Center => User32.DT_CENTER,
            TextAlignment.Far => User32.DT_RIGHT,
            _ => User32.DT_LEFT,
        };

        if ((flags & TextFormatFlags.NoPrefix) != 0)
        {
            format |= User32.DT_NOPREFIX;
        }

        bool noWrap = (flags & TextFormatFlags.NoWrap) != 0 || !finiteWidth;
        format |= noWrap ? User32.DT_SINGLELINE : User32.DT_WORDBREAK;
        return format;
    }

    private static bool IsIdentityTransform(in Matrix3x2 m)
        => MathF.Abs(m.M11 - 1f) < 0.0001f &&
           MathF.Abs(m.M22 - 1f) < 0.0001f &&
           MathF.Abs(m.M12) < 0.0001f &&
           MathF.Abs(m.M21) < 0.0001f &&
           MathF.Abs(m.M31) < 0.0001f &&
           MathF.Abs(m.M32) < 0.0001f;

    private unsafe bool TryDrawTextAsPath(int x, int y, string text, ColorF color)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        if (!Gdi32.BeginPath(_hdcMem))
            return false;

        bool wrote;
        fixed (char* pText = text)
        {
            wrote = Gdi32.TextOutRaw(_hdcMem, x, y, pText, text.Length);
        }

        if (!wrote)
        {
            Gdi32.EndPath(_hdcMem);
            return false;
        }

        if (!Gdi32.EndPath(_hdcMem))
            return false;

        nint hBrush = Gdi32.CreateSolidBrush(ToColorRef(color));
        nint hOldBrush = Gdi32.SelectObject(_hdcMem, hBrush);
        nint hNullPen = Gdi32.GetStockObject(Gdi32.NULL_PEN);
        nint hOldPen = Gdi32.SelectObject(_hdcMem, hNullPen);

        bool ok = Gdi32.FillPath(_hdcMem);

        Gdi32.SelectObject(_hdcMem, hOldPen);
        Gdi32.SelectObject(_hdcMem, hOldBrush);
        Gdi32.DeleteObject(hBrush);
        return ok;
    }

    /// Manual x adjustment for ExtTextOut/TextOut paths which don't support alignment natively.
    private unsafe void AdjustXForAlignment(ref int x, in DrawCommand cmd, int drawWidth)
    {
        if (cmd.TextAlignment == TextAlignment.Near || float.IsPositiveInfinity(cmd.MaxWidth))
            return;
        if (string.IsNullOrEmpty(cmd.Text))
            return;

        string text = cmd.Text;

        int measuredWidth = drawWidth;
        if (measuredWidth <= 0)
        {
            fixed (char* pMeasure = text)
            {
                Gdi32.GetTextExtentPoint32Raw(_hdcMem, pMeasure, text.Length, out SIZE textSize);
                measuredWidth = textSize.cx;
            }
        }

        float layoutWidth = cmd.MaxWidth;
        if (cmd.TextAlignment == TextAlignment.Center)
            x += (int)((layoutWidth - measuredWidth) / 2f);
        else // Far
            x += (int)(layoutWidth - measuredWidth);
    }

    public float GetFontHeight(FontSpec font)
    {
        if (_hdcMem == 0) return 0f;

        nint hFont = GetOrCreateGdiFont(font);
        nint hOldFont = Gdi32.SelectObject(_hdcMem, hFont);

        Gdi32.GetTextMetrics(_hdcMem, out TEXTMETRIC tm);

        Gdi32.SelectObject(_hdcMem, hOldFont);

        return tm.tmHeight;
    }

    public bool TryCopyPixelsBgra(out int width, out int height, out byte[] bgraPixels)
    {
        width = _width;
        height = _height;
        if (_bitmapBits == 0 || _width <= 0 || _height <= 0)
        {
            width = 0;
            height = 0;
            bgraPixels = [];
            return false;
        }

        int byteCount = _width * _height * 4;
        bgraPixels = new byte[byteCount];
        Marshal.Copy(_bitmapBits, bgraPixels, 0, byteCount);
        return true;
    }

    // --- Helpers ---

    private void CreateBackBuffer(int width, int height)
    {
        _width = width;
        _height = height;

        if (_hdcMem == 0)
            _hdcMem = Gdi32.CreateCompatibleDC(_hdcWindow);

        CreateDIBSection(width, height);
    }

    private void CreateDIBSection(int width, int height)
    {
        _width = width;
        _height = height;

        BITMAPINFO bmi = new();
        bmi.bmiHeader.biSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<BITMAPINFOHEADER>();
        bmi.bmiHeader.biWidth = width;
        bmi.bmiHeader.biHeight = -height; // top-down
        bmi.bmiHeader.biPlanes = 1;
        bmi.bmiHeader.biBitCount = 32;
        bmi.bmiHeader.biCompression = 0; // BI_RGB

        _hBitmap = Gdi32.CreateDIBSection(_hdcMem, ref bmi, Gdi32.DIB_RGB_COLORS, out _bitmapBits, 0, 0);
        _hBitmapOld = Gdi32.SelectObject(_hdcMem, _hBitmap);
    }

    private void DestroyBackBuffer()
    {
        if (_hBitmapOld != 0)
        {
            Gdi32.SelectObject(_hdcMem, _hBitmapOld);
            _hBitmapOld = 0;
        }
        if (_hBitmap != 0)
        {
            Gdi32.DeleteObject(_hBitmap);
            _hBitmap = 0;
        }
    }

    private void QueryDeviceDpi()
    {
        var dpi = _hdcWindow != 0 ? Gdi32.GetDeviceCaps(_hdcWindow, Gdi32.LOGPIXELSY) : 96;
        _deviceDpi = dpi > 0 ? dpi : 96f;
    }

    private nint CreateGdiFont(FontSpec font)
    {
        int weight = font.Weight switch
        {
            FontWeight.Thin => 100,
            FontWeight.Light => 300,
            FontWeight.Normal => 400,
            FontWeight.Medium => 500,
            FontWeight.SemiBold => 600,
            FontWeight.Bold => 700,
            FontWeight.ExtraBold => 800,
            FontWeight.Black => 900,
            _ => 400,
        };

        // Use constant 96 DPI base — the Canvas DPI world transform handles
        // physical scaling via TrueType re-rasterization at the transformed size.
        const int baseDpi = 96;
        var heightPx = -(int)MathF.Round(font.Size * baseDpi / 72f);
        if (heightPx == 0)
        {
            heightPx = -1;
        }

        return Gdi32.CreateFont(
            cHeight: heightPx, // points to pixels at 96 DPI logical base
            cWidth: 0,
            cEscapement: 0,
            cOrientation: 0,
            cWeight: weight,
            bItalic: font.Style == FontStyle.Italic ? 1u : 0u,
            bUnderline: 0,
            bStrikeOut: 0,
            iCharSet: Gdi32.DEFAULT_CHARSET,
            iOutPrecision: Gdi32.OUT_TT_PRECIS,
            iClipPrecision: Gdi32.CLIP_DEFAULT_PRECIS,
            iQuality: _fontQuality,
            iPitchAndFamily: Gdi32.DEFAULT_PITCH,
            pszFaceName: font.FamilyName);
    }

    private static uint ResolveFontQuality(SmoothingMode smoothing, CompositingQuality compositing)
    {
        if (smoothing == SmoothingMode.None)
        {
            return Gdi32.NONANTIALIASED_QUALITY;
        }

        if (compositing == CompositingQuality.HighSpeed)
        {
            return Gdi32.ANTIALIASED_QUALITY;
        }

        return Gdi32.CLEARTYPE_QUALITY;
    }

    private nint GetOrCreateGdiFont(FontSpec font)
    {
        if (_fontCache.TryGetValue(font, out nint hFont) && hFont != 0)
        {
            return hFont;
        }

        hFont = CreateGdiFont(font);
        if (hFont != 0)
        {
            _fontCache[font] = hFont;
        }

        return hFont;
    }

    private void DisposeFontCache()
    {
        foreach (nint hFont in _fontCache.Values)
        {
            if (hFont != 0)
            {
                Gdi32.DeleteObject(hFont);
            }
        }

        _fontCache.Clear();
    }

    /// <summary>
    /// Converts ColorF to GDI COLORREF (0x00BBGGRR).
    /// </summary>
    private static uint ToColorRef(ColorF c)
    {
        byte r = (byte)(Math.Clamp(c.R, 0f, 1f) * 255f + 0.5f);
        byte g = (byte)(Math.Clamp(c.G, 0f, 1f) * 255f + 0.5f);
        byte b = (byte)(Math.Clamp(c.B, 0f, 1f) * 255f + 0.5f);
        return (uint)(r | (g << 8) | (b << 16));
    }

    private static int ToGdiPenStyle(DashStyle style) => style switch
    {
        DashStyle.Solid => Gdi32.PS_SOLID,
        DashStyle.Dash => Gdi32.PS_DASH,
        DashStyle.Dot => Gdi32.PS_DOT,
        DashStyle.DashDot => Gdi32.PS_DASHDOT,
        DashStyle.DashDotDot => Gdi32.PS_DASHDOTDOT,
        _ => Gdi32.PS_SOLID,
    };

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DisposeFontCache();

        if (_isExternalHdc)
        {
            // External HDC: do not free — caller owns it
            _hdcMem = 0;
            return;
        }

        DestroyBackBuffer();

        if (_hdcMem != 0)
        {
            Gdi32.DeleteDC(_hdcMem);
            _hdcMem = 0;
        }

        if (!_isOffscreen && _hwnd != 0 && _hdcWindow != 0)
        {
            User32.ReleaseDC(_hwnd, _hdcWindow);
            _hdcWindow = 0;
        }
    }
}
