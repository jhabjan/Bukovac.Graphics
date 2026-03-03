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
using Bukovac.Graphics.Rasterizers.Windows.OpenGL;

namespace Bukovac.Graphics.Rasterizers.Windows;

/// <summary>
/// Windows OpenGL rasterizer.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class OpenGLRasterizer : IRasterizer
{
    private readonly IRasterizer _bitmapBackend = new GdiRasterizer();
    private readonly IRasterizer _textBackend = new GdiRasterizer();
    private readonly Dictionary<int, uint> _imageTextures = new();
    private readonly Dictionary<TextCacheKey, TextCacheValue> _textTextures = new();
    private readonly Stack<RenderState> _stateStack = new();
    private RenderState _state = RenderState.Default;
    private bool _bitmapTarget;
    private bool _offscreenGl;
    private bool _ownsHwnd;
    private nint _hwnd;
    private nint _hdc;
    private nint _glContext;
    private int _width;
    private int _height;
    private float _dpi = 96f;
    private byte[]? _lastPixelsBgra;
    private int _textureFilter = (int)Wgl32.GL_LINEAR;
    private bool _lineSmoothing = true;
    private bool _dither = true;
    private float _pixelOffsetX;
    private float _pixelOffsetY;

    public void Initialize(NativeWindowHandle window, int width, int height)
    {
        if (window.Kind != "HWND")
        {
            throw new ArgumentException($"OpenGlRasterizer requires HWND, got '{window.Kind}'.");
        }

        _bitmapTarget = false;
        _offscreenGl = false;
        _ownsHwnd = false;
        _hwnd = window.Handle;
        _width = width;
        _height = height;

        if (!TryInitializeOpenGlWindow())
        {
            throw new PlatformNotSupportedException("Failed to initialize Windows OpenGL (WGL).");
        }
        _textBackend.InitializeBitmap(width, height);

    }

    public void InitializeBitmap(int width, int height)
    {
        _bitmapTarget = false;
        _offscreenGl = true;
        _width = width;
        _height = height;

        _hwnd = WinUser32.CreateWindowEx(0, "STATIC", string.Empty, WinUser32.WS_POPUP, 0, 0, width, height, 0, 0, 0, 0);
        if (_hwnd == 0)
        {
            throw new PlatformNotSupportedException("Failed to create hidden window for OpenGL bitmap mode.");
        }

        _ownsHwnd = true;
        if (!TryInitializeOpenGlWindow())
        {
            throw new PlatformNotSupportedException("Failed to initialize OpenGL bitmap mode.");
        }
        _textBackend.InitializeBitmap(width, height);

    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        if (_bitmapTarget)
        {
            _bitmapBackend.Resize(width, height);
            return;
        }
        _textBackend.Resize(width, height);

        if (_offscreenGl && _ownsHwnd && _hwnd != 0)
        {
            WinUser32.MoveWindow(_hwnd, 0, 0, width, height, false);
        }

        if (_glContext != 0 && _hdc != 0)
        {
            Wgl32.wglMakeCurrent(_hdc, _glContext);
            Wgl32.glViewport(0, 0, width, height);
        }
    }

    public void BeginFrame()
    {
        if (_bitmapTarget)
        {
            _bitmapBackend.BeginFrame();
            return;
        }

        _state = RenderState.Default;
        _stateStack.Clear();
        _lastPixelsBgra = null;
    }

    public void EndFrame(ReadOnlySpan<DrawCommand> commands)
    {
        if (_bitmapTarget)
        {
            _bitmapBackend.EndFrame(commands);
            return;
        }

        if (_glContext == 0 || _hdc == 0)
        {
            return;
        }

        if (!Wgl32.wglMakeCurrent(_hdc, _glContext))
        {
            return;
        }

        SetupFrame();
        ExecuteCommands(commands);
        CapturePixels();

        Wgl32.glFlush();
        if (!_offscreenGl)
        {
            WinGdi32.SwapBuffers(_hdc);
        }
    }

    public Vector2 MeasureString(string text, FontSpec font, float maxWidth)
    {
        return _textBackend.MeasureString(text, font, maxWidth);
    }

    public Vector2 MeasureString(string text, FontSpec font, float maxWidth, TextFormatFlags flags)
    {
        return _textBackend.MeasureString(text, font, maxWidth, flags);
    }

    public float GetFontHeight(FontSpec font)
    {
        return _textBackend.GetFontHeight(font);
    }

    public float GetDpi()
    {
        if (_bitmapTarget)
        {
            return _bitmapBackend.GetDpi();
        }

        return _dpi;
    }

    public void SetDpi(float dpi)
    {
        if (_bitmapTarget)
        {
            _bitmapBackend.SetDpi(dpi);
            return;
        }

        _dpi = dpi;
        _textBackend.SetDpi(dpi);
    }

    public void ApplyQualitySettings(InterpolationMode interpolation, SmoothingMode smoothing,
        PixelOffsetMode pixelOffset, CompositingQuality compositing)
    {
        if (_bitmapTarget)
        {
            _bitmapBackend.ApplyQualitySettings(interpolation, smoothing, pixelOffset, compositing);
            return;
        }
        _textBackend.ApplyQualitySettings(interpolation, smoothing, pixelOffset, compositing);

        _textureFilter = interpolation switch
        {
            InterpolationMode.NearestNeighbor => (int)Wgl32.GL_NEAREST,
            _ => (int)Wgl32.GL_LINEAR,
        };

        _lineSmoothing = smoothing switch
        {
            SmoothingMode.None => false,
            _ => true,
        };

        (_pixelOffsetX, _pixelOffsetY) = pixelOffset switch
        {
            PixelOffsetMode.Half => (0.5f, 0.5f),
            _ => (0f, 0f),
        };

        _dither = compositing switch
        {
            CompositingQuality.HighSpeed => false,
            _ => true,
        };
    }

    public void InitializeFromHdc(nint hdc, int width, int height)
    {
        InitializeFromHdc(hdc, 0, 0, width, height);
    }

    public void InitializeFromHdc(nint hdc, int x, int y, int width, int height)
    {
        _bitmapTarget = true;
        _width = width;
        _height = height;
        _bitmapBackend.InitializeFromHdc(hdc, x, y, width, height);
    }

    public bool TryCopyPixelsBgra(out int width, out int height, out byte[] bgraPixels)
    {
        if (_bitmapTarget)
        {
            return _bitmapBackend.TryCopyPixelsBgra(out width, out height, out bgraPixels);
        }

        if (_lastPixelsBgra is not null)
        {
            width = _width;
            height = _height;
            bgraPixels = _lastPixelsBgra;
            return true;
        }

        width = 0;
        height = 0;
        bgraPixels = [];
        return false;
    }

    public void Dispose()
    {
        if (_glContext != 0 && _hdc != 0)
        {
            Wgl32.wglMakeCurrent(_hdc, _glContext);
            foreach (uint tex in _imageTextures.Values)
            {
                uint texture = tex;
                Wgl32.glDeleteTextures(1, ref texture);
            }
            _imageTextures.Clear();
            foreach (var entry in _textTextures.Values)
            {
                uint texture = entry.TextureId;
                Wgl32.glDeleteTextures(1, ref texture);
            }
            _textTextures.Clear();
        }

        if (_glContext != 0)
        {
            Wgl32.wglMakeCurrent(0, 0);
            Wgl32.wglDeleteContext(_glContext);
            _glContext = 0;
        }

        if (_hdc != 0 && _hwnd != 0)
        {
            WinUser32.ReleaseDC(_hwnd, _hdc);
            _hdc = 0;
        }

        if (_ownsHwnd && _hwnd != 0)
        {
            WinUser32.DestroyWindow(_hwnd);
            _hwnd = 0;
            _ownsHwnd = false;
        }

        _bitmapBackend.Dispose();
        _textBackend.Dispose();
    }

    private bool TryInitializeOpenGlWindow()
    {
        try
        {
            _hdc = WinUser32.GetDC(_hwnd);
            if (_hdc == 0)
            {
                return false;
            }

            var pfd = new PIXELFORMATDESCRIPTOR
            {
                nSize = (ushort)Marshal.SizeOf<PIXELFORMATDESCRIPTOR>(),
                nVersion = 1,
                dwFlags = WinGdi32.PFD_DRAW_TO_WINDOW | WinGdi32.PFD_SUPPORT_OPENGL | WinGdi32.PFD_DOUBLEBUFFER,
                iPixelType = WinGdi32.PFD_TYPE_RGBA,
                cColorBits = 32,
                cDepthBits = 24,
                cStencilBits = 8,
                iLayerType = WinGdi32.PFD_MAIN_PLANE,
            };

            int pixelFormat = WinGdi32.ChoosePixelFormat(_hdc, ref pfd);
            if (pixelFormat == 0)
            {
                return false;
            }

            if (!WinGdi32.SetPixelFormat(_hdc, pixelFormat, ref pfd))
            {
                return false;
            }

            _glContext = Wgl32.wglCreateContext(_hdc);
            if (_glContext == 0)
            {
                return false;
            }

            if (!Wgl32.wglMakeCurrent(_hdc, _glContext))
            {
                return false;
            }

            Wgl32.glViewport(0, 0, _width, _height);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void SetupFrame()
    {
        Wgl32.glViewport(0, 0, _width, _height);
        Wgl32.glClearColor(0f, 0f, 0f, 1f);
        Wgl32.glClear(Wgl32.GL_COLOR_BUFFER_BIT);

        Wgl32.glMatrixMode(Wgl32.GL_PROJECTION);
        Wgl32.glLoadIdentity();
        Wgl32.glOrtho(0, _width, _height, 0, -1, 1);
        Wgl32.glMatrixMode(Wgl32.GL_MODELVIEW);
        Wgl32.glLoadIdentity();

        Wgl32.glEnable(Wgl32.GL_BLEND);
        Wgl32.glBlendFunc(Wgl32.GL_SRC_ALPHA, Wgl32.GL_ONE_MINUS_SRC_ALPHA);
        if (_lineSmoothing)
        {
            Wgl32.glEnable(Wgl32.GL_LINE_SMOOTH);
        }
        else
        {
            Wgl32.glDisable(Wgl32.GL_LINE_SMOOTH);
        }
        if (_dither)
        {
            Wgl32.glEnable(Wgl32.GL_DITHER);
        }
        else
        {
            Wgl32.glDisable(Wgl32.GL_DITHER);
        }
        Wgl32.glDisable(Wgl32.GL_SCISSOR_TEST);
    }

    private void ExecuteCommands(ReadOnlySpan<DrawCommand> commands)
    {
        for (int i = 0; i < commands.Length; i++)
        {
            ref readonly DrawCommand cmd = ref commands[i];

            switch (cmd.Kind)
            {
                case DrawCommandKind.Clear:
                    Wgl32.glClearColor(cmd.Color.R, cmd.Color.G, cmd.Color.B, cmd.Color.A);
                    Wgl32.glClear(Wgl32.GL_COLOR_BUFFER_BIT);
                    break;
                case DrawCommandKind.Save:
                    _stateStack.Push(_state);
                    break;
                case DrawCommandKind.Restore:
                    _state = _stateStack.Count > 0 ? _stateStack.Pop() : RenderState.Default;
                    ApplyClip();
                    break;
                case DrawCommandKind.SetTransform:
                    _state.Transform = cmd.Transform;
                    break;
                case DrawCommandKind.ResetTransform:
                    _state.Transform = Matrix3x2.Identity;
                    break;
                case DrawCommandKind.SetClip:
                    _state.Clip = cmd.Rect;
                    ApplyClip();
                    break;
                case DrawCommandKind.ResetClip:
                    _state.Clip = null;
                    ApplyClip();
                    break;
                case DrawCommandKind.DrawLine:
                    DrawLine(in cmd);
                    break;
                case DrawCommandKind.DrawRectangle:
                    DrawRectangle(in cmd);
                    break;
                case DrawCommandKind.FillRectangle:
                    FillRectangle(in cmd);
                    break;
                case DrawCommandKind.DrawEllipse:
                    DrawEllipse(in cmd, false);
                    break;
                case DrawCommandKind.FillEllipse:
                    DrawEllipse(in cmd, true);
                    break;
                case DrawCommandKind.DrawRoundedRectangle:
                    DrawRoundedRectangle(in cmd, false);
                    break;
                case DrawCommandKind.FillRoundedRectangle:
                    DrawRoundedRectangle(in cmd, true);
                    break;
                case DrawCommandKind.DrawImage:
                    DrawImage(in cmd);
                    break;
                case DrawCommandKind.DrawString:
                    DrawString(in cmd);
                    break;
            }
        }
    }

    private void CapturePixels()
    {
        if (_width <= 0 || _height <= 0)
        {
            _lastPixelsBgra = null;
            return;
        }

        int bytes = _width * _height * 4;
        var pixels = new byte[bytes];
        unsafe
        {
            fixed (byte* ptr = pixels)
            {
                Wgl32.glReadPixels(0, 0, _width, _height, Wgl32.GL_BGRA, Wgl32.GL_UNSIGNED_BYTE, (nint)ptr);
            }
        }

        FlipRowsInPlace(pixels, _width, _height);
        _lastPixelsBgra = pixels;
    }

    private void DrawLine(in DrawCommand cmd)
    {
        SetColor(cmd.Color);
        Wgl32.glLineWidth(MathF.Max(1f, cmd.StrokeWidth));
        PointF p0 = TransformPoint(cmd.P1);
        PointF p1 = TransformPoint(cmd.P2);

        if (cmd.DashStyle == DashStyle.Solid)
        {
            Wgl32.glBegin(Wgl32.GL_LINES);
            Wgl32.glVertex2f(p0.X, p0.Y);
            Wgl32.glVertex2f(p1.X, p1.Y);
            Wgl32.glEnd();
            return;
        }

        DrawDashedLine(p0, p1, cmd.DashStyle, cmd.StrokeWidth);
    }

    private void DrawRectangle(in DrawCommand cmd)
    {
        SetColor(cmd.Color);
        Wgl32.glLineWidth(MathF.Max(1f, cmd.StrokeWidth));
        RectF r = cmd.Rect;
        DrawLineLoop(
            TransformPoint(new PointF(r.X, r.Y)),
            TransformPoint(new PointF(r.Right, r.Y)),
            TransformPoint(new PointF(r.Right, r.Bottom)),
            TransformPoint(new PointF(r.X, r.Bottom)));
    }

    private void FillRectangle(in DrawCommand cmd)
    {
        SetColor(cmd.Color);
        RectF r = cmd.Rect;
        DrawQuad(
            TransformPoint(new PointF(r.X, r.Y)),
            TransformPoint(new PointF(r.Right, r.Y)),
            TransformPoint(new PointF(r.Right, r.Bottom)),
            TransformPoint(new PointF(r.X, r.Bottom)));
    }

    private void DrawEllipse(in DrawCommand cmd, bool fill)
    {
        SetColor(cmd.Color);
        if (!fill)
        {
            Wgl32.glLineWidth(MathF.Max(1f, cmd.StrokeWidth));
        }

        RectF r = cmd.Rect;
        int segments = 64;
        float cx = r.X + (r.Width * 0.5f);
        float cy = r.Y + (r.Height * 0.5f);
        float rx = r.Width * 0.5f;
        float ry = r.Height * 0.5f;
        Wgl32.glBegin(fill ? Wgl32.GL_TRIANGLE_FAN : Wgl32.GL_LINE_LOOP);
        for (int i = 0; i < segments; i++)
        {
            float t = (i / (float)segments) * (MathF.PI * 2f);
            PointF p = TransformPoint(new PointF(cx + (MathF.Cos(t) * rx), cy + (MathF.Sin(t) * ry)));
            Wgl32.glVertex2f(p.X, p.Y);
        }
        Wgl32.glEnd();
    }

    private void DrawRoundedRectangle(in DrawCommand cmd, bool fill)
    {
        SetColor(cmd.Color);
        if (!fill)
        {
            Wgl32.glLineWidth(MathF.Max(1f, cmd.StrokeWidth));
        }

        RectF r = cmd.Rect;
        float radius = MathF.Min(cmd.CornerRadius, MathF.Min(r.Width, r.Height) * 0.5f);
        int arcSegments = 12;
        var points = new List<PointF>(arcSegments * 4);
        AppendCorner(points, r.Right - radius, r.Y + radius, radius, -MathF.PI / 2f, 0f, arcSegments);
        AppendCorner(points, r.Right - radius, r.Bottom - radius, radius, 0f, MathF.PI / 2f, arcSegments);
        AppendCorner(points, r.X + radius, r.Bottom - radius, radius, MathF.PI / 2f, MathF.PI, arcSegments);
        AppendCorner(points, r.X + radius, r.Y + radius, radius, MathF.PI, MathF.PI * 1.5f, arcSegments);

        Wgl32.glBegin(fill ? Wgl32.GL_TRIANGLE_FAN : Wgl32.GL_LINE_LOOP);
        for (int i = 0; i < points.Count; i++)
        {
            PointF p = TransformPoint(points[i]);
            Wgl32.glVertex2f(p.X, p.Y);
        }
        Wgl32.glEnd();
    }

    private void DrawImage(in DrawCommand cmd)
    {
        if (!ImageManager.TryGetImageData(cmd.Image, out ImageData data))
        {
            return;
        }

        _imageTextures.TryGetValue(cmd.Image.Id, out uint texture);

        RectF? src = cmd.HasSrcRect ? cmd.SrcRect : null;
        DrawTexture(ref texture, data.Width, data.Height, data.BgraPixels, cmd.Rect, src, cmd.Opacity <= 0 ? 1f : cmd.Opacity);
        _imageTextures[cmd.Image.Id] = texture;
    }

    private void DrawString(in DrawCommand cmd)
    {
        if (string.IsNullOrEmpty(cmd.Text))
        {
            return;
        }
        var key = new TextCacheKey(
            cmd.Text!,
            cmd.Font.FamilyName,
            cmd.Font.Size,
            cmd.Font.Weight,
            cmd.Font.Style,
            cmd.Color.ToArgb32(),
            cmd.MaxWidth,
            cmd.TextAlignment,
            cmd.TextFlags,
            cmd.TextRenderMode);

        if (!_textTextures.TryGetValue(key, out TextCacheValue entry))
        {
            entry = BuildTextTextureEntry(in cmd);
            _textTextures[key] = entry;
        }

        float x = cmd.P1.X;
        if (!float.IsPositiveInfinity(cmd.MaxWidth))
        {
            float dx = MathF.Max(0f, cmd.MaxWidth - entry.ContentWidth);
            if (cmd.TextAlignment == TextAlignment.Center)
            {
                x += dx * 0.5f;
            }
            else if (cmd.TextAlignment == TextAlignment.Far)
            {
                x += dx;
            }
        }

        float textOpacity = cmd.TextRenderMode == TextRenderMode.AlphaAccurate
            ? Math.Clamp(cmd.Color.A, 0f, 1f)
            : 1f;
        DrawTextureCached(entry.TextureId, entry.TexWidth, entry.TexHeight, new PointF(x, cmd.P1.Y), entry.ContentWidth, entry.ContentHeight, textOpacity);
    }

    private TextCacheValue BuildTextTextureEntry(in DrawCommand cmd)
    {
        Vector2 measured = MeasureTextForTexture(in cmd);
        int texW = Math.Max(1, (int)MathF.Ceiling(measured.X) + 2);
        int texH = Math.Max(1, (int)MathF.Ceiling(measured.Y) + 2);

        _textBackend.Resize(texW, texH);
        _textBackend.BeginFrame();
        var textCmd = new DrawCommand
        {
            Kind = DrawCommandKind.DrawString,
            Text = cmd.Text,
            Font = cmd.Font,
            Color = cmd.Color,
            P1 = new PointF(0, 0),
            MaxWidth = cmd.MaxWidth,
            TextAlignment = TextAlignment.Near,
            TextFlags = cmd.TextFlags,
            TextRenderMode = cmd.TextRenderMode,
            GlyphAdvances = cmd.GlyphAdvances,
            GlyphUniformAdvance = cmd.GlyphUniformAdvance,
        };
        _textBackend.EndFrame([DrawCommand.Clear(ColorF.Transparent), textCmd]);

        if (!_textBackend.TryCopyPixelsBgra(out int w, out int h, out byte[] pixels))
        {
            return new TextCacheValue(0, 1, 1, 1, 1);
        }

        NormalizeTextPixels(pixels);
        uint texture = 0;
        EnsureTextureUploaded(ref texture, w, h, pixels);
        return new TextCacheValue(texture, w, h, measured.X, measured.Y);
    }

    private Vector2 MeasureTextForTexture(in DrawCommand cmd)
    {
        string text = cmd.Text!;
        if (text.Length == 0)
        {
            return Vector2.Zero;
        }

        if (cmd.GlyphAdvances is { Length: > 0 } advances)
        {
            int limit = Math.Min(advances.Length, text.Length);
            int width = 0;
            for (int i = 0; i < limit; i++)
            {
                width += advances[i];
            }

            float height = MathF.Max(1f, _textBackend.GetFontHeight(cmd.Font));
            return new Vector2(width, height);
        }

        if (cmd.GlyphUniformAdvance > 0)
        {
            float width = cmd.GlyphUniformAdvance * text.Length;
            float height = MathF.Max(1f, _textBackend.GetFontHeight(cmd.Font));
            return new Vector2(width, height);
        }

        if (text.IndexOfAny(['\r', '\n']) < 0)
        {
            return _textBackend.MeasureString(text, cmd.Font, cmd.MaxWidth, cmd.TextFlags);
        }

        float maxWidth = 0f;
        int lineCount = 0;
        int start = 0;
        while (start <= text.Length)
        {
            int end = start;
            while (end < text.Length && text[end] != '\r' && text[end] != '\n')
            {
                end++;
            }

            string line = text[start..end];
            if (line.Length > 0)
            {
                Vector2 lineSize = _textBackend.MeasureString(line, cmd.Font, float.PositiveInfinity, cmd.TextFlags | TextFormatFlags.NoWrap);
                maxWidth = MathF.Max(maxWidth, lineSize.X);
            }

            lineCount++;
            if (end >= text.Length)
            {
                break;
            }

            if (text[end] == '\r' && end + 1 < text.Length && text[end + 1] == '\n')
            {
                start = end + 2;
            }
            else
            {
                start = end + 1;
            }
        }

        float lineHeight = MathF.Max(1f, _textBackend.GetFontHeight(cmd.Font));
        return new Vector2(maxWidth, lineHeight * lineCount);
    }

    private static void NormalizeTextPixels(byte[] pixels)
    {
        for (int i = 0; i < pixels.Length; i += 4)
        {
            byte b = pixels[i + 0];
            byte g = pixels[i + 1];
            byte r = pixels[i + 2];
            byte a = pixels[i + 3];

            byte promoted = Math.Max(a, Math.Max(r, Math.Max(g, b)));
            pixels[i + 3] = promoted;
        }
    }

    private void EnsureTextureUploaded(ref uint texture, int texWidth, int texHeight, byte[] bgraPixels)
    {
        if (texture == 0)
        {
            Wgl32.glGenTextures(1, out texture);
        }

        Wgl32.glBindTexture(Wgl32.GL_TEXTURE_2D, texture);
        Wgl32.glTexParameteri(Wgl32.GL_TEXTURE_2D, Wgl32.GL_TEXTURE_MIN_FILTER, _textureFilter);
        Wgl32.glTexParameteri(Wgl32.GL_TEXTURE_2D, Wgl32.GL_TEXTURE_MAG_FILTER, _textureFilter);
        Wgl32.glTexParameteri(Wgl32.GL_TEXTURE_2D, Wgl32.GL_TEXTURE_WRAP_S, (int)Wgl32.GL_CLAMP_TO_EDGE);
        Wgl32.glTexParameteri(Wgl32.GL_TEXTURE_2D, Wgl32.GL_TEXTURE_WRAP_T, (int)Wgl32.GL_CLAMP_TO_EDGE);

        unsafe
        {
            fixed (byte* pPixels = bgraPixels)
            {
                Wgl32.glTexImage2D(
                    Wgl32.GL_TEXTURE_2D,
                    0,
                    Wgl32.GL_RGBA8,
                    texWidth,
                    texHeight,
                    0,
                    Wgl32.GL_BGRA,
                    Wgl32.GL_UNSIGNED_BYTE,
                    (nint)pPixels);
            }
        }
    }

    private void DrawTextureCached(uint texture, int texWidth, int texHeight, PointF origin, float width, float height, float opacity)
    {
        if (texture == 0)
        {
            return;
        }

        Wgl32.glEnable(Wgl32.GL_TEXTURE_2D);
        Wgl32.glBindTexture(Wgl32.GL_TEXTURE_2D, texture);

        float u1 = texWidth > 0 ? Math.Clamp(width / texWidth, 0f, 1f) : 1f;
        float v1 = texHeight > 0 ? Math.Clamp(height / texHeight, 0f, 1f) : 1f;

        PointF p0 = TransformPoint(origin);
        PointF p1 = TransformPoint(new PointF(origin.X + width, origin.Y));
        PointF p2 = TransformPoint(new PointF(origin.X + width, origin.Y + height));
        PointF p3 = TransformPoint(new PointF(origin.X, origin.Y + height));

        Wgl32.glColor4f(1f, 1f, 1f, Math.Clamp(opacity, 0f, 1f));
        Wgl32.glBegin(Wgl32.GL_QUADS);
        Wgl32.glTexCoord2f(0f, 0f); Wgl32.glVertex2f(p0.X, p0.Y);
        Wgl32.glTexCoord2f(u1, 0f); Wgl32.glVertex2f(p1.X, p1.Y);
        Wgl32.glTexCoord2f(u1, v1); Wgl32.glVertex2f(p2.X, p2.Y);
        Wgl32.glTexCoord2f(0f, v1); Wgl32.glVertex2f(p3.X, p3.Y);
        Wgl32.glEnd();
        Wgl32.glDisable(Wgl32.GL_TEXTURE_2D);
    }

    private void DrawTexture(ref uint texture, int texWidth, int texHeight, byte[] bgraPixels, RectF dest, RectF? src, float opacity)
    {
        EnsureTextureUploaded(ref texture, texWidth, texHeight, bgraPixels);
        Wgl32.glEnable(Wgl32.GL_TEXTURE_2D);
        Wgl32.glBindTexture(Wgl32.GL_TEXTURE_2D, texture);

        float u0 = 0f;
        float v0 = 0f;
        float u1 = 1f;
        float v1 = 1f;
        if (src.HasValue)
        {
            RectF s = src.Value;
            u0 = s.X / texWidth;
            v0 = s.Y / texHeight;
            u1 = s.Right / texWidth;
            v1 = s.Bottom / texHeight;
        }

        PointF p0 = TransformPoint(new PointF(dest.X, dest.Y));
        PointF p1 = TransformPoint(new PointF(dest.Right, dest.Y));
        PointF p2 = TransformPoint(new PointF(dest.Right, dest.Bottom));
        PointF p3 = TransformPoint(new PointF(dest.X, dest.Bottom));

        Wgl32.glColor4f(1f, 1f, 1f, Math.Clamp(opacity, 0f, 1f));
        Wgl32.glBegin(Wgl32.GL_QUADS);

        Wgl32.glTexCoord2f(u0, v0);
        Wgl32.glVertex2f(p0.X, p0.Y);

        Wgl32.glTexCoord2f(u1, v0);
        Wgl32.glVertex2f(p1.X, p1.Y);

        Wgl32.glTexCoord2f(u1, v1);
        Wgl32.glVertex2f(p2.X, p2.Y);

        Wgl32.glTexCoord2f(u0, v1);
        Wgl32.glVertex2f(p3.X, p3.Y);

        Wgl32.glEnd();
        Wgl32.glDisable(Wgl32.GL_TEXTURE_2D);
    }

    private void DrawDashedLine(PointF p0, PointF p1, DashStyle style, float strokeWidth)
    {
        float[] pattern = style switch
        {
            DashStyle.Dash => [6f, 4f],
            DashStyle.Dot => [1f, 3f],
            DashStyle.DashDot => [6f, 3f, 1f, 3f],
            DashStyle.DashDotDot => [6f, 3f, 1f, 3f, 1f, 3f],
            _ => [1000f, 0f],
        };

        float length = MathF.Sqrt(((p1.X - p0.X) * (p1.X - p0.X)) + ((p1.Y - p0.Y) * (p1.Y - p0.Y)));
        if (length <= 0.001f)
        {
            return;
        }

        float nx = (p1.X - p0.X) / length;
        float ny = (p1.Y - p0.Y) / length;
        float t = 0f;
        int idx = 0;
        bool draw = true;
        while (t < length)
        {
            float seg = pattern[idx % pattern.Length] * MathF.Max(1f, strokeWidth);
            float t0 = t;
            float t1 = MathF.Min(length, t + seg);
            if (draw)
            {
                Wgl32.glBegin(Wgl32.GL_LINES);
                Wgl32.glVertex2f(p0.X + (nx * t0), p0.Y + (ny * t0));
                Wgl32.glVertex2f(p0.X + (nx * t1), p0.Y + (ny * t1));
                Wgl32.glEnd();
            }

            draw = !draw;
            idx++;
            t = t1;
        }
    }

    private void DrawLineLoop(PointF p0, PointF p1, PointF p2, PointF p3)
    {
        Wgl32.glBegin(Wgl32.GL_LINE_LOOP);
        Wgl32.glVertex2f(p0.X, p0.Y);
        Wgl32.glVertex2f(p1.X, p1.Y);
        Wgl32.glVertex2f(p2.X, p2.Y);
        Wgl32.glVertex2f(p3.X, p3.Y);
        Wgl32.glEnd();
    }

    private void DrawQuad(PointF p0, PointF p1, PointF p2, PointF p3)
    {
        Wgl32.glBegin(Wgl32.GL_QUADS);
        Wgl32.glVertex2f(p0.X, p0.Y);
        Wgl32.glVertex2f(p1.X, p1.Y);
        Wgl32.glVertex2f(p2.X, p2.Y);
        Wgl32.glVertex2f(p3.X, p3.Y);
        Wgl32.glEnd();
    }

    private void ApplyClip()
    {
        if (!_state.Clip.HasValue)
        {
            Wgl32.glDisable(Wgl32.GL_SCISSOR_TEST);
            return;
        }

        RectF clip = _state.Clip.Value;
        PointF p0 = TransformPoint(new PointF(clip.X, clip.Y));
        PointF p1 = TransformPoint(new PointF(clip.Right, clip.Y));
        PointF p2 = TransformPoint(new PointF(clip.Right, clip.Bottom));
        PointF p3 = TransformPoint(new PointF(clip.X, clip.Bottom));

        float minX = MathF.Min(MathF.Min(p0.X, p1.X), MathF.Min(p2.X, p3.X));
        float maxX = MathF.Max(MathF.Max(p0.X, p1.X), MathF.Max(p2.X, p3.X));
        float minY = MathF.Min(MathF.Min(p0.Y, p1.Y), MathF.Min(p2.Y, p3.Y));
        float maxY = MathF.Max(MathF.Max(p0.Y, p1.Y), MathF.Max(p2.Y, p3.Y));

        int left = Math.Clamp((int)MathF.Floor(minX), 0, _width);
        int right = Math.Clamp((int)MathF.Ceiling(maxX), 0, _width);
        int top = Math.Clamp((int)MathF.Floor(minY), 0, _height);
        int bottom = Math.Clamp((int)MathF.Ceiling(maxY), 0, _height);

        int w = Math.Max(0, right - left);
        int h = Math.Max(0, bottom - top);
        if (w == 0 || h == 0)
        {
            Wgl32.glDisable(Wgl32.GL_SCISSOR_TEST);
            return;
        }

        int glY = _height - bottom;
        Wgl32.glEnable(Wgl32.GL_SCISSOR_TEST);
        Wgl32.glScissor(left, glY, w, h);
    }

    private PointF TransformPoint(PointF point)
    {
        Vector2 transformed = Vector2.Transform(new Vector2(point.X, point.Y), _state.Transform);
        return new PointF(transformed.X + _pixelOffsetX, transformed.Y + _pixelOffsetY);
    }

    private static void SetColor(ColorF color)
    {
        Wgl32.glColor4f(color.R, color.G, color.B, color.A);
    }

    private static void AppendCorner(List<PointF> points, float cx, float cy, float r, float start, float end, int segments)
    {
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float a = start + ((end - start) * t);
            points.Add(new PointF(cx + (MathF.Cos(a) * r), cy + (MathF.Sin(a) * r)));
        }
    }

    private static void FlipRowsInPlace(byte[] pixels, int width, int height)
    {
        int stride = width * 4;
        var temp = new byte[stride];
        int half = height / 2;
        for (int y = 0; y < half; y++)
        {
            int top = y * stride;
            int bottom = (height - 1 - y) * stride;
            Buffer.BlockCopy(pixels, top, temp, 0, stride);
            Buffer.BlockCopy(pixels, bottom, pixels, top, stride);
            Buffer.BlockCopy(temp, 0, pixels, bottom, stride);
        }
    }

    private struct RenderState
    {
        public Matrix3x2 Transform;
        public RectF? Clip;

        public static RenderState Default => new()
        {
            Transform = Matrix3x2.Identity,
            Clip = null,
        };
    }

    private readonly record struct TextCacheKey(
        string Text,
        string FontFamily,
        float FontSize,
        FontWeight Weight,
        FontStyle Style,
        uint ColorArgb,
        float MaxWidth,
        TextAlignment Alignment,
        TextFormatFlags Flags,
        TextRenderMode RenderMode);

    private readonly record struct TextCacheValue(
        uint TextureId,
        int TexWidth,
        int TexHeight,
        float ContentWidth,
        float ContentHeight);

}
