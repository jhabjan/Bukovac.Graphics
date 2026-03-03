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
using Bukovac.Graphics.Commands;

namespace Bukovac.Graphics;

/// <summary>
/// Primary drawing surface. Records draw commands and dispatches to a platform rasterizer.
/// Can target a native OS window or an off-screen bitmap.
/// </summary>
public sealed class Canvas : IDisposable
{
    private readonly IRasterizer _rasterizer;
    private readonly CommandList _commands;
    private bool _disposed;
    private int _width;
    private int _height;
    private float _dpiScale = 1.0f;
    private Matrix3x2 _dpiTransform = Matrix3x2.Identity;

    /// <summary>
    /// Creates a Canvas with the specified rasterizer kind.
    /// Falls back to <see cref="GraphicsConfig.RasterizerKind"/> if Default is used.
    /// </summary>
    public Canvas(RasterizerKind rasterizerKind = RasterizerKind.Default)
    {
        _rasterizer = RasterizerFactory.Create(rasterizerKind);
        _commands = new CommandList();
    }

    /// <summary>
    /// Creates a Canvas with an externally provided rasterizer instance.
    /// </summary>
    public Canvas(IRasterizer rasterizer)
    {
        _rasterizer = rasterizer ?? throw new ArgumentNullException(nameof(rasterizer));
        _commands = new CommandList();
    }

    public int Width => _width;
    public int Height => _height;

    /// <summary>
    /// The DPI scale factor (e.g. 1.0 at 96 DPI, 1.5 at 144 DPI).
    /// </summary>
    public float DpiScale => _dpiScale;

    public InterpolationMode InterpolationMode { get; set; } = InterpolationMode.Default;
    public SmoothingMode SmoothingMode { get; set; } = SmoothingMode.Default;
    public PixelOffsetMode PixelOffsetMode { get; set; } = PixelOffsetMode.Default;
    public CompositingQuality CompositingQuality { get; set; } = CompositingQuality.Default;

    /// <summary>
    /// Width in DPI-logical pixels (physical width / DPI scale).
    /// </summary>
    public float LogicalWidth => _width / _dpiScale;

    /// <summary>
    /// Height in DPI-logical pixels (physical height / DPI scale).
    /// </summary>
    public float LogicalHeight => _height / _dpiScale;

    /// <summary>
    /// Convert a physical pixel value to DPI-logical coordinates.
    /// </summary>
    public float ToLogical(int physicalValue) => physicalValue / _dpiScale;

    /// <summary>
    /// Convert a DPI-logical value to physical pixels.
    /// </summary>
    public float ToPhysical(float logicalValue) => logicalValue * _dpiScale;

    /// <summary>
    /// Creates a Canvas that renders into an existing GDI HDC from a native paint callback.
    /// The caller is responsible for releasing the HDC after calling EndFrame/Dispose.
    /// </summary>
    public static Canvas FromGraphics(nint hdc, int width, int height, float dpi = 96f)
        => FromGraphics(RasterizerKind.WindowsGDI, hdc, 0, 0, width, height, dpi);

    /// <summary>
    /// Creates a Canvas that renders into a sub-rectangle of an existing native graphics context.
    /// The caller is responsible for releasing the HDC after calling EndFrame/Dispose.
    /// </summary>
    public static Canvas FromGraphics(RasterizerKind rasterizerKind, nint hdc, int x, int y, int width, int height, float dpi = 96f)
    {
        var canvas = new Canvas(rasterizerKind);
        canvas._width = width;
        canvas._height = height;
        canvas._rasterizer.InitializeFromHdc(hdc, x, y, width, height);
        canvas.SetDpiScale(dpi);
        return canvas;
    }

    // --- Lifecycle ---

    /// <summary>
    /// Initialize for rendering to a native OS window.
    /// </summary>
    public void Initialize(NativeWindowHandle window, int width, int height)
    {
        _width = width;
        _height = height;
        _rasterizer.Initialize(window, width, height);
        UpdateDpiScale();
    }

    /// <summary>
    /// Initialize for rendering to a native OS window with explicit host DPI.
    /// </summary>
    public void Initialize(NativeWindowHandle window, int width, int height, float dpi)
    {
        _width = width;
        _height = height;
        _rasterizer.Initialize(window, width, height);
        _rasterizer.SetDpi(dpi);
        SetDpiScale(dpi);
    }

    /// <summary>
    /// Initialize for rendering to an off-screen bitmap.
    /// When dpi is 96 (default), no DPI scaling is applied.
    /// </summary>
    public void Initialize(int width, int height, float dpi = 96f)
    {
        _width = width;
        _height = height;
        _rasterizer.InitializeBitmap(width, height);
        SetDpiScale(dpi);
    }

    /// <summary>
    /// Handle window/surface resize.
    /// </summary>
    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        _rasterizer.Resize(width, height);
    }

    /// <summary>
    /// Begin recording a new frame.
    /// </summary>
    public void BeginFrame()
    {
        _commands.Clear();
        _rasterizer.BeginFrame();
        _rasterizer.ApplyQualitySettings(InterpolationMode, SmoothingMode, PixelOffsetMode, CompositingQuality);
    }

    /// <summary>
    /// Finish the frame: execute all recorded commands via the rasterizer and present.
    /// </summary>
    public void EndFrame()
    {
        _rasterizer.EndFrame(_commands.AsSpan());
    }

    // --- Drawing operations ---

    public void Clear(ColorF color)
    {
        _commands.Add(DrawCommand.Clear(color));
    }

    // --- State ---

    public void Save()
    {
        _commands.Add(DrawCommand.SaveState());
    }

    public void Restore()
    {
        _commands.Add(DrawCommand.RestoreState());
    }

    public void SetTransform(Matrix3x2 transform)
    {
        _commands.Add(DrawCommand.SetTransformCmd(transform));
    }

    public void ResetTransform()
    {
        _commands.Add(DrawCommand.ResetTransformCmd());
    }

    public void SetClip(RectF rect)
    {
        _commands.Add(DrawCommand.SetClipCmd(rect));
    }

    public void ResetClip()
    {
        _commands.Add(DrawCommand.ResetClipCmd());
    }

    // --- Shapes ---

    public void DrawLine(Pen pen, PointF p1, PointF p2)
    {
        _commands.Add(DrawCommand.Line(pen, p1, p2));
    }

    public void DrawLine(Pen pen, float x1, float y1, float x2, float y2)
    {
        DrawLine(pen, new PointF(x1, y1), new PointF(x2, y2));
    }

    public void DrawRectangle(Pen pen, RectF rect)
    {
        _commands.Add(DrawCommand.DrawRect(pen, rect));
    }

    public void DrawRectangle(Pen pen, float x, float y, float width, float height)
    {
        DrawRectangle(pen, new RectF(x, y, width, height));
    }

    public void FillRectangle(Brush brush, RectF rect)
    {
        _commands.Add(DrawCommand.FillRect(brush, rect));
    }

    public void FillRectangle(Brush brush, float x, float y, float width, float height)
    {
        FillRectangle(brush, new RectF(x, y, width, height));
    }

    public void DrawEllipse(Pen pen, RectF rect)
    {
        _commands.Add(DrawCommand.DrawEllipseCmd(pen, rect));
    }

    public void DrawEllipse(Pen pen, float x, float y, float width, float height)
    {
        DrawEllipse(pen, new RectF(x, y, width, height));
    }

    public void FillEllipse(Brush brush, RectF rect)
    {
        _commands.Add(DrawCommand.FillEllipseCmd(brush, rect));
    }

    public void FillEllipse(Brush brush, float x, float y, float width, float height)
    {
        FillEllipse(brush, new RectF(x, y, width, height));
    }

    public void DrawLines(Pen pen, ReadOnlySpan<PointF> points)
    {
        if (points.Length < 2) return;
        for (int i = 0; i < points.Length - 1; i++)
        {
            DrawLine(pen, points[i], points[i + 1]);
        }
    }

    public void DrawLines(Pen pen, params PointF[] points)
    {
        DrawLines(pen, points.AsSpan());
    }

    public void DrawPolygon(Pen pen, ReadOnlySpan<PointF> points)
    {
        if (points.Length < 2) return;
        DrawLines(pen, points);
        DrawLine(pen, points[^1], points[0]);
    }

    public void DrawPolygon(Pen pen, params PointF[] points)
    {
        DrawPolygon(pen, points.AsSpan());
    }

    public void FillPolygon(Brush brush, ReadOnlySpan<PointF> points)
    {
        if (points.Length < 3)
        {
            return;
        }

        float minY = points[0].Y;
        float maxY = points[0].Y;

        for (int i = 1; i < points.Length; i++)
        {
            minY = MathF.Min(minY, points[i].Y);
            maxY = MathF.Max(maxY, points[i].Y);
        }

        int startY = (int)MathF.Floor(minY);
        int endY = (int)MathF.Ceiling(maxY);

        if (endY <= startY)
        {
            return;
        }

        var intersections = new List<float>(points.Length);

        for (int y = startY; y < endY; y++)
        {
            float scanY = y + 0.5f;
            intersections.Clear();

            for (int i = 0; i < points.Length; i++)
            {
                PointF a = points[i];
                PointF b = points[(i + 1) % points.Length];
                bool crosses = (a.Y <= scanY && b.Y > scanY) || (b.Y <= scanY && a.Y > scanY);
                if (!crosses) continue;

                float t = (scanY - a.Y) / (b.Y - a.Y);
                intersections.Add(a.X + (t * (b.X - a.X)));
            }

            if (intersections.Count < 2)
            {
                continue;
            }

            intersections.Sort();

            for (int i = 0; i + 1 < intersections.Count; i += 2)
            {
                float x0 = intersections[i];
                float x1 = intersections[i + 1];
                if (x1 > x0)
                {
                    FillRectangle(brush, x0, y, x1 - x0, 1f);
                }
            }
        }
    }

    public void FillPolygon(Brush brush, params PointF[] points)
    {
        FillPolygon(brush, points.AsSpan());
    }

    public void DrawArc(Pen pen, RectF rect, float startAngle, float sweepAngle)
    {
        List<PointF> arcPoints = BuildArcPoints(rect, startAngle, sweepAngle);
        DrawLines(pen, arcPoints.ToArray());
    }

    public void DrawArc(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle)
    {
        DrawArc(pen, new RectF(x, y, width, height), startAngle, sweepAngle);
    }

    public void DrawPie(Pen pen, RectF rect, float startAngle, float sweepAngle)
    {
        List<PointF> pie = BuildArcPoints(rect, startAngle, sweepAngle);

        if (pie.Count == 0)
        {
            return;
        }

        PointF center = rect.Center;
        DrawLine(pen, center, pie[0]);
        DrawLines(pen, pie.ToArray());
        DrawLine(pen, pie[^1], center);
    }

    public void DrawPie(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle)
    {
        DrawPie(pen, new RectF(x, y, width, height), startAngle, sweepAngle);
    }

    public void FillPie(Brush brush, RectF rect, float startAngle, float sweepAngle)
    {
        List<PointF> arcPoints = BuildArcPoints(rect, startAngle, sweepAngle);

        if (arcPoints.Count == 0)
        {
            return;
        }

        PointF center = rect.Center;
        var pie = new PointF[arcPoints.Count + 2];
        pie[0] = center;
        for (int i = 0; i < arcPoints.Count; i++)
        {
            pie[i + 1] = arcPoints[i];
        }
        pie[^1] = center;

        FillPolygon(brush, pie);
    }

    public void FillPie(Brush brush, float x, float y, float width, float height, float startAngle, float sweepAngle)
    {
        FillPie(brush, new RectF(x, y, width, height), startAngle, sweepAngle);
    }

    public void DrawBezier(Pen pen, PointF p1, PointF p2, PointF p3, PointF p4)
    {
        float approxLength = Distance(p1, p2) + Distance(p2, p3) + Distance(p3, p4);

        int segments = Math.Clamp((int)MathF.Ceiling(approxLength / 6f), 8, 128);

        PointF prev = p1;

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            PointF next = EvaluateCubicBezier(p1, p2, p3, p4, t);
            DrawLine(pen, prev, next);
            prev = next;
        }
    }

    public void DrawBezier(Pen pen, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
    {
        DrawBezier(pen,
            new PointF(x1, y1),
            new PointF(x2, y2),
            new PointF(x3, y3),
            new PointF(x4, y4));
    }

    public void DrawBeziers(Pen pen, ReadOnlySpan<PointF> points)
    {
        if (points.Length < 4)
        {
            return;
        }

        int curveCount = (points.Length - 1) / 3;
        
        for (int i = 0; i < curveCount; i++)
        {
            int index = i * 3;
            DrawBezier(pen, points[index], points[index + 1], points[index + 2], points[index + 3]);
        }
    }

    public void DrawBeziers(Pen pen, params PointF[] points)
    {
        DrawBeziers(pen, points.AsSpan());
    }

    // --- Rounded Rectangles ---

    public void DrawRoundedRectangle(Pen pen, RectF rect, float cornerRadius)
    {
        _commands.Add(DrawCommand.DrawRoundedRect(pen, rect, cornerRadius));
    }

    public void DrawRoundedRectangle(Pen pen, float x, float y, float width, float height, float cornerRadius)
    {
        DrawRoundedRectangle(pen, new RectF(x, y, width, height), cornerRadius);
    }

    public void FillRoundedRectangle(Brush brush, RectF rect, float cornerRadius)
    {
        _commands.Add(DrawCommand.FillRoundedRect(brush, rect, cornerRadius));
    }

    public void FillRoundedRectangle(Brush brush, float x, float y, float width, float height, float cornerRadius)
    {
        FillRoundedRectangle(brush, new RectF(x, y, width, height), cornerRadius);
    }

    // --- Text ---

    public Vector2 MeasureString(string text, FontSpec font, float maxWidth = float.PositiveInfinity)
    {
        // Text measurement is synchronous — goes straight to the rasterizer
        return _rasterizer.MeasureString(text, font, maxWidth);
    }

    public void DrawString(string text, FontSpec font, Brush brush, PointF origin, float maxWidth = float.PositiveInfinity, TextRenderMode renderMode = TextRenderMode.Default)
    {
        _commands.Add(DrawCommand.String(text, font, brush, origin, maxWidth, TextAlignment.Near, TextFormatFlags.None, renderMode));
    }

    public void DrawGlyphRun(string text, int[] glyphAdvances, FontSpec font, Brush brush, PointF origin, float maxWidth = float.PositiveInfinity, TextRenderMode renderMode = TextRenderMode.Default)
    {
        _commands.Add(DrawCommand.StringWithAdvances(text, glyphAdvances, font, brush, origin, maxWidth, TextAlignment.Near, TextFormatFlags.None, renderMode));
    }

    public void DrawGlyphRun(string text, int[] glyphAdvances, FontSpec font, Brush brush, float x, float y, float maxWidth = float.PositiveInfinity, TextRenderMode renderMode = TextRenderMode.Default)
    {
        DrawGlyphRun(text, glyphAdvances, font, brush, new PointF(x, y), maxWidth, renderMode);
    }

    public void DrawGlyphRunUniform(string text, int glyphUniformAdvance, FontSpec font, Brush brush, PointF origin, float maxWidth = float.PositiveInfinity, TextRenderMode renderMode = TextRenderMode.Default)
    {
        _commands.Add(DrawCommand.StringWithUniformAdvance(text, glyphUniformAdvance, font, brush, origin, maxWidth, TextAlignment.Near, TextFormatFlags.None, renderMode));
    }

    public void DrawGlyphRunUniform(string text, int glyphUniformAdvance, FontSpec font, Brush brush, float x, float y, float maxWidth = float.PositiveInfinity, TextRenderMode renderMode = TextRenderMode.Default)
    {
        DrawGlyphRunUniform(text, glyphUniformAdvance, font, brush, new PointF(x, y), maxWidth, renderMode);
    }

    public void DrawString(string text, FontSpec font, Brush brush, float x, float y, float maxWidth = float.PositiveInfinity, TextRenderMode renderMode = TextRenderMode.Default)
    {
        DrawString(text, font, brush, new PointF(x, y), maxWidth, renderMode);
    }

    public void DrawString(string text, FontSpec font, Brush brush, PointF origin, TextFormatFlags flags, float maxWidth = float.PositiveInfinity, TextRenderMode renderMode = TextRenderMode.Default)
    {
        _commands.Add(DrawCommand.String(text, font, brush, origin, maxWidth, TextAlignment.Near, flags, renderMode));
    }

    public void DrawString(string text, FontSpec font, Brush brush, RectF layoutRect, TextAlignment alignment = TextAlignment.Near, TextFormatFlags flags = TextFormatFlags.None, TextRenderMode renderMode = TextRenderMode.Default)
    {
        _commands.Add(DrawCommand.String(text, font, brush, new PointF(layoutRect.X, layoutRect.Y), layoutRect.Width, alignment, flags, renderMode));
    }

    public Vector2 MeasureString(string text, FontSpec font, TextFormatFlags flags, float maxWidth = float.PositiveInfinity)
    {
        return _rasterizer.MeasureString(text, font, maxWidth, flags);
    }

    public float GetFontHeight(FontSpec font) => _rasterizer.GetFontHeight(font);

    // --- Measurement Canvas ---

    public static Canvas CreateMeasurementCanvas(RasterizerKind kind = RasterizerKind.Default)
    {
        var canvas = new Canvas(kind);
        canvas.Initialize(1, 1);
        return canvas;
    }

    // --- Images ---

    /// <summary>
    /// Register a 32-bit BGRA pixel buffer as an image and return a handle for drawing.
    /// </summary>
    public ImageHandle LoadImage(int width, int height, byte[] bgraPixels) => ImageManager.Register(width, height, bgraPixels);

    public void DrawImage(ImageHandle image, RectF dest, RectF? src = null, float opacity = 1f)
    {
        _commands.Add(DrawCommand.DrawImageCmd(image, dest, src, opacity));
    }

    public void DrawImage(ImageHandle image, float x, float y)
    {
        if (!ImageManager.TryGetImageData(image, out var data)) return;
        DrawImage(image, new RectF(x, y, data.Width, data.Height));
    }

    public void DrawImage(ImageHandle image, float x, float y, float width, float height)
    {
        DrawImage(image, new RectF(x, y, width, height));
    }

    public void DrawImage(ImageHandle image, RectF dest, RectF src)
    {
        DrawImage(image, dest, src, 1f);
    }

    /// <summary>
    /// Attempts to read the current off-screen surface as top-down BGRA pixels.
    /// </summary>
    public bool TryGetPixelsBgra(out int width, out int height, out byte[] bgraPixels) => _rasterizer.TryCopyPixelsBgra(out width, out height, out bgraPixels);

    /// <summary>
    /// Attempts to capture pixels from the current canvas, using a fallback CPU rasterizer when direct readback is unavailable.
    /// </summary>
    public bool TryCapturePixelsBgra(out int width, out int height, out byte[] bgraPixels)
    {
        if (TryGetPixelsBgra(out width, out height, out bgraPixels))
        {
            return true;
        }

        return TryGetPixelsViaFallbackRasterizer(out width, out height, out bgraPixels);
    }

    /// <summary>
    /// Saves current canvas contents to an image file.
    /// Supported formats: png, bmp, gif, jpg/jpeg.
    /// </summary>
    public void SaveImage(string path, ImageFileFormat? format = null, int jpegQuality = 90)
    {
        if (!TryCapturePixelsBgra(out int width, out int height, out byte[] pixels))
        {
            throw new InvalidOperationException("Rasterizer does not support pixel readback for this canvas.");
        }

        ImageFileFormat actualFormat = format ?? ImageEncoding.DetectFormatFromPath(path);
        ImageEncoding.Save(path, actualFormat, width, height, pixels, jpegQuality);
    }

    // --- DPI ---

    private void UpdateDpiScale()
    {
        SetDpiScale(_rasterizer.GetDpi());
    }

    private void SetDpiScale(float dpi)
    {
        _dpiScale = dpi / 96f;

        _dpiTransform = _dpiScale != 1.0f
            ? Matrix3x2.CreateScale(_dpiScale)
            : Matrix3x2.Identity;
    }

    /// <summary>
    /// Updates DPI from host (e.g. per-monitor DPI change).
    /// </summary>
    public void SetDpi(float dpi)
    {
        _rasterizer.SetDpi(dpi);
        SetDpiScale(dpi);
    }

    // --- Dispose ---

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _rasterizer.Dispose();
    }

    private static List<PointF> BuildArcPoints(RectF rect, float startAngle, float sweepAngle)
    {
        if (rect.Width <= 0f || rect.Height <= 0f || sweepAngle == 0f)
        {
            return [];
        }

        float absSweep = MathF.Abs(sweepAngle);
        int segments = Math.Clamp((int)MathF.Ceiling(absSweep / 8f), 2, 256);
        float cx = rect.X + (rect.Width * 0.5f);
        float cy = rect.Y + (rect.Height * 0.5f);
        float rx = rect.Width * 0.5f;
        float ry = rect.Height * 0.5f;

        var points = new List<PointF>(segments + 1);
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = (startAngle + (sweepAngle * t)) * (MathF.PI / 180f);
            float x = cx + (MathF.Cos(angle) * rx);
            float y = cy + (MathF.Sin(angle) * ry);
            points.Add(new PointF(x, y));
        }

        return points;
    }

    private static PointF EvaluateCubicBezier(PointF p1, PointF p2, PointF p3, PointF p4, float t)
    {
        float it = 1f - t;
        float it2 = it * it;
        float t2 = t * t;

        float b1 = it2 * it;
        float b2 = 3f * it2 * t;
        float b3 = 3f * it * t2;
        float b4 = t2 * t;

        return new PointF(
            (p1.X * b1) + (p2.X * b2) + (p3.X * b3) + (p4.X * b4),
            (p1.Y * b1) + (p2.Y * b2) + (p3.Y * b3) + (p4.Y * b4));
    }

    private static float Distance(PointF a, PointF b)
    {
        float dx = b.X - a.X;
        float dy = b.Y - a.Y;
        return MathF.Sqrt((dx * dx) + (dy * dy));
    }

    private bool TryGetPixelsViaFallbackRasterizer(out int width, out int height, out byte[] bgraPixels)
    {
        width = 0;
        height = 0;
        bgraPixels = [];

        if (_width <= 0 || _height <= 0)
        {
            return false;
        }

        RasterizerKind fallbackKind;
        if (OperatingSystem.IsWindows())
        {
            fallbackKind = RasterizerKind.WindowsGDI;
        }
        else if (OperatingSystem.IsLinux())
        {
            fallbackKind = RasterizerKind.LinuxCairo;
        }
        else if (OperatingSystem.IsMacOS())
        {
            fallbackKind = RasterizerKind.MacCoreGraphics;
        }
        else
        {
            return false;
        }

        using IRasterizer fallback = RasterizerFactory.Create(fallbackKind);
        fallback.InitializeBitmap(_width, _height);
        fallback.BeginFrame();
        fallback.ApplyQualitySettings(InterpolationMode, SmoothingMode, PixelOffsetMode, CompositingQuality);
        fallback.EndFrame(_commands.AsSpan());
        return fallback.TryCopyPixelsBgra(out width, out height, out bgraPixels);
    }
}
