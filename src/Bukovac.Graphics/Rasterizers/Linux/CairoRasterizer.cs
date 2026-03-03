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
using System.Collections.Generic;
using Bukovac.Graphics.Commands;

namespace Bukovac.Graphics.Rasterizers.Linux;

/// <summary>
/// Cairo-based CPU rasterizer for Linux. Uses an X11 surface for window rendering
/// or an image surface for off-screen bitmap rendering.
/// </summary>
[SupportedOSPlatform("linux")]
public sealed class CairoRasterizer : IRasterizer
{
    private nint _surface;
    private nint _xSurface;  // X11 window surface (windowed mode only)
    private nint _cr;
    private int _width;
    private int _height;
    private bool _disposed;
    private bool _isOffscreen;
    private int _imageFilter = Cairo.CAIRO_FILTER_BILINEAR;
    private int _antialias = Cairo.CAIRO_ANTIALIAS_BEST;
    private int _compositeOperator = Cairo.CAIRO_OPERATOR_OVER;
    private double _pixelOffsetX;
    private double _pixelOffsetY;

    public void Initialize(NativeWindowHandle window, int width, int height)
    {
        if (window.Kind != "X11")
            throw new ArgumentException($"CairoRasterizer requires X11, got '{window.Kind}'");

        _isOffscreen = false;
        _width = width;
        _height = height;

        // X11 surface for presenting to the window
        _xSurface = Cairo.cairo_xlib_surface_create(
            window.Display, window.Handle, window.Visual, width, height);

        // Image back buffer — all rendering goes here to avoid flicker
        _surface = Cairo.cairo_image_surface_create(Cairo.CAIRO_FORMAT_ARGB32, width, height);
        _cr = Cairo.cairo_create(_surface);
    }

    public void InitializeBitmap(int width, int height)
    {
        _isOffscreen = true;
        _width = width;
        _height = height;

        _surface = Cairo.cairo_image_surface_create(Cairo.CAIRO_FORMAT_ARGB32, width, height);
        _cr = Cairo.cairo_create(_surface);
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;

        // Recreate the image back buffer at the new size
        Cairo.cairo_destroy(_cr);
        Cairo.cairo_surface_destroy(_surface);
        _surface = Cairo.cairo_image_surface_create(Cairo.CAIRO_FORMAT_ARGB32, width, height);
        _cr = Cairo.cairo_create(_surface);

        if (!_isOffscreen && _xSurface != 0)
        {
            Cairo.cairo_xlib_surface_set_size(_xSurface, width, height);
        }
    }

    public void BeginFrame()
    {
        if (_cr != 0)
        {
            Cairo.cairo_set_antialias(_cr, _antialias);
            Cairo.cairo_set_operator(_cr, _compositeOperator);
            if (_pixelOffsetX != 0.0 || _pixelOffsetY != 0.0)
            {
                Cairo.cairo_translate(_cr, _pixelOffsetX, _pixelOffsetY);
            }
        }
    }

    public void EndFrame(ReadOnlySpan<DrawCommand> commands)
    {
        ExecuteCommands(commands);
        Cairo.cairo_surface_flush(_surface);

        // Blit back buffer to the X11 window surface in one
        // atomic operation — eliminates flicker.
        if (!_isOffscreen && _xSurface != 0)
        {
            nint crWindow = Cairo.cairo_create(_xSurface);
            Cairo.cairo_set_source_surface(crWindow, _surface, 0, 0);
            Cairo.cairo_paint(crWindow);
            Cairo.cairo_destroy(crWindow);
            Cairo.cairo_surface_flush(_xSurface);
        }
    }

    public Vector2 MeasureString(string text, FontSpec font, float maxWidth)
    {
        if (_cr == 0) return Vector2.Zero;

        SetCairoFont(_cr, font);
        Cairo.cairo_text_extents(_cr, text, out CairoTextExtents extents);
        // Use x_advance (logical advance width) instead of width (tight glyph bbox)
        // and font cell height (ascent+descent) instead of per-glyph height.
        // This matches GDI GetTextExtentPoint32 behavior: consistent height for
        // a given font regardless of string content, and proper advance spacing.
        Cairo.cairo_font_extents(_cr, out CairoFontExtents fontExtents);
        return new Vector2((float)extents.x_advance, (float)(fontExtents.ascent + fontExtents.descent));
    }

    public Vector2 MeasureString(string text, FontSpec font, float maxWidth, TextFormatFlags flags)
    {
        if (_cr == 0 || string.IsNullOrEmpty(text))
        {
            return Vector2.Zero;
        }

        SetCairoFont(_cr, font);
        Cairo.cairo_font_extents(_cr, out CairoFontExtents fontExtents);
        float lineHeight = (float)Math.Max(1.0, fontExtents.ascent + fontExtents.descent);

        bool finiteWidth = !float.IsPositiveInfinity(maxWidth);
        bool wrap = finiteWidth && (flags & TextFormatFlags.NoWrap) == 0;
        var lines = BuildWrappedLines(text, wrap ? maxWidth : float.PositiveInfinity, MeasureCairoWidth);

        float maxLineWidth = 0f;
        for (int i = 0; i < lines.Count; i++)
        {
            maxLineWidth = Math.Max(maxLineWidth, (float)MeasureCairoWidth(lines[i]));
        }

        return new Vector2(maxLineWidth, lineHeight * lines.Count);
    }

    public void ApplyQualitySettings(InterpolationMode interpolation, SmoothingMode smoothing,
        PixelOffsetMode pixelOffset, CompositingQuality compositing)
    {
        _imageFilter = interpolation switch
        {
            InterpolationMode.NearestNeighbor => Cairo.CAIRO_FILTER_NEAREST,
            InterpolationMode.HighQualityBicubic => Cairo.CAIRO_FILTER_BEST,
            InterpolationMode.Bicubic => Cairo.CAIRO_FILTER_GOOD,
            _ => Cairo.CAIRO_FILTER_BILINEAR,
        };

        _antialias = smoothing switch
        {
            SmoothingMode.None => Cairo.CAIRO_ANTIALIAS_NONE,
            SmoothingMode.HighQuality => Cairo.CAIRO_ANTIALIAS_BEST,
            SmoothingMode.AntiAlias => Cairo.CAIRO_ANTIALIAS_GOOD,
            _ => Cairo.CAIRO_ANTIALIAS_DEFAULT,
        };

        (_pixelOffsetX, _pixelOffsetY) = pixelOffset switch
        {
            PixelOffsetMode.Half => (0.5, 0.5),
            _ => (0.0, 0.0),
        };

        _compositeOperator = compositing switch
        {
            CompositingQuality.HighSpeed => Cairo.CAIRO_OPERATOR_SOURCE,
            _ => Cairo.CAIRO_OPERATOR_OVER,
        };
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
                    Cairo.cairo_save(_cr);
                    break;
                case DrawCommandKind.Restore:
                    Cairo.cairo_restore(_cr);
                    break;
                case DrawCommandKind.SetClip:
                    Cairo.cairo_rectangle(_cr, cmd.Rect.X, cmd.Rect.Y, cmd.Rect.Width, cmd.Rect.Height);
                    Cairo.cairo_clip(_cr);
                    break;
                case DrawCommandKind.ResetClip:
                    Cairo.cairo_reset_clip(_cr);
                    break;
                case DrawCommandKind.SetTransform:
                    ExecuteSetTransform(in cmd);
                    break;
                case DrawCommandKind.ResetTransform:
                    Cairo.cairo_identity_matrix(_cr);
                    if (_pixelOffsetX != 0.0 || _pixelOffsetY != 0.0)
                    {
                        Cairo.cairo_translate(_cr, _pixelOffsetX, _pixelOffsetY);
                    }
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
        Cairo.cairo_save(_cr);
        Cairo.cairo_set_source_rgba(_cr, color.R, color.G, color.B, color.A);
        Cairo.cairo_paint(_cr);
        Cairo.cairo_restore(_cr);
    }

    private void ExecuteSetTransform(in DrawCommand cmd)
    {
        var m = new CairoMatrix
        {
            xx = cmd.Transform.M11,
            yx = cmd.Transform.M12,
            xy = cmd.Transform.M21,
            yy = cmd.Transform.M22,
            x0 = cmd.Transform.M31 + _pixelOffsetX,
            y0 = cmd.Transform.M32 + _pixelOffsetY,
        };
        Cairo.cairo_set_matrix(_cr, ref m);
    }

    private void ExecuteDrawLine(in DrawCommand cmd)
    {
        Cairo.cairo_set_source_rgba(_cr, cmd.Color.R, cmd.Color.G, cmd.Color.B, cmd.Color.A);
        Cairo.cairo_set_line_width(_cr, cmd.StrokeWidth);
        SetCairoDash(_cr, cmd.DashStyle, cmd.StrokeWidth);
        Cairo.cairo_move_to(_cr, cmd.P1.X, cmd.P1.Y);
        Cairo.cairo_line_to(_cr, cmd.P2.X, cmd.P2.Y);
        Cairo.cairo_stroke(_cr);
    }

    private void ExecuteDrawRectangle(in DrawCommand cmd)
    {
        Cairo.cairo_set_source_rgba(_cr, cmd.Color.R, cmd.Color.G, cmd.Color.B, cmd.Color.A);
        Cairo.cairo_set_line_width(_cr, cmd.StrokeWidth);
        SetCairoDash(_cr, cmd.DashStyle, cmd.StrokeWidth);
        Cairo.cairo_rectangle(_cr, cmd.Rect.X, cmd.Rect.Y, cmd.Rect.Width, cmd.Rect.Height);
        Cairo.cairo_stroke(_cr);
    }

    private void ExecuteFillRectangle(in DrawCommand cmd)
    {
        Cairo.cairo_set_source_rgba(_cr, cmd.Color.R, cmd.Color.G, cmd.Color.B, cmd.Color.A);
        Cairo.cairo_rectangle(_cr, cmd.Rect.X, cmd.Rect.Y, cmd.Rect.Width, cmd.Rect.Height);
        Cairo.cairo_fill(_cr);
    }

    private void ExecuteDrawEllipse(in DrawCommand cmd)
    {
        double cx = cmd.Rect.X + cmd.Rect.Width / 2.0;
        double cy = cmd.Rect.Y + cmd.Rect.Height / 2.0;
        double rx = cmd.Rect.Width / 2.0;
        double ry = cmd.Rect.Height / 2.0;

        // Build ellipse path using scale trick
        Cairo.cairo_save(_cr);
        Cairo.cairo_translate(_cr, cx, cy);
        Cairo.cairo_scale(_cr, rx, ry);
        Cairo.cairo_arc(_cr, 0, 0, 1.0, 0, 2 * Math.PI);
        Cairo.cairo_restore(_cr);

        // Stroke with unscaled line width
        Cairo.cairo_set_source_rgba(_cr, cmd.Color.R, cmd.Color.G, cmd.Color.B, cmd.Color.A);
        Cairo.cairo_set_line_width(_cr, cmd.StrokeWidth);
        SetCairoDash(_cr, cmd.DashStyle, cmd.StrokeWidth);
        Cairo.cairo_stroke(_cr);
    }

    private void ExecuteFillEllipse(in DrawCommand cmd)
    {
        double cx = cmd.Rect.X + cmd.Rect.Width / 2.0;
        double cy = cmd.Rect.Y + cmd.Rect.Height / 2.0;
        double rx = cmd.Rect.Width / 2.0;
        double ry = cmd.Rect.Height / 2.0;

        Cairo.cairo_save(_cr);
        Cairo.cairo_translate(_cr, cx, cy);
        Cairo.cairo_scale(_cr, rx, ry);
        Cairo.cairo_arc(_cr, 0, 0, 1.0, 0, 2 * Math.PI);
        Cairo.cairo_restore(_cr);

        Cairo.cairo_set_source_rgba(_cr, cmd.Color.R, cmd.Color.G, cmd.Color.B, cmd.Color.A);
        Cairo.cairo_fill(_cr);
    }

    private void ExecuteDrawString(in DrawCommand cmd)
    {
        if (cmd.Text is null) return;

        SetCairoFont(_cr, cmd.Font);
        Cairo.cairo_set_source_rgba(_cr, cmd.Color.R, cmd.Color.G, cmd.Color.B, cmd.Color.A);

        double x = cmd.P1.X;
        double y = cmd.P1.Y + cmd.Font.Size;

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
                double layoutWidth = cmd.MaxWidth;
                if (cmd.TextAlignment == TextAlignment.Center)
                    x += (layoutWidth - drawWidth) / 2.0;
                else
                    x += layoutWidth - drawWidth;
            }

            int index = 0;
            while (index < cmd.Text.Length)
            {
                int glyphLen = 1;
                if (char.IsHighSurrogate(cmd.Text[index]) &&
                    index + 1 < cmd.Text.Length &&
                    char.IsLowSurrogate(cmd.Text[index + 1]))
                {
                    glyphLen = 2;
                }

                int glyphAdvance = 0;
                int end = Math.Min(index + glyphLen, limit);
                for (int i = index; i < end; i++)
                {
                    glyphAdvance += advances[i];
                }

                string glyph = cmd.Text.Substring(index, glyphLen);
                Cairo.cairo_move_to(_cr, x, y);
                Cairo.cairo_show_text(_cr, glyph);
                x += glyphAdvance;
                index += glyphLen;
            }
            return;
        }

        if (cmd.GlyphUniformAdvance > 0)
        {
            int drawWidth = cmd.GlyphUniformAdvance * cmd.Text.Length;
            if (cmd.TextAlignment != TextAlignment.Near && !float.IsPositiveInfinity(cmd.MaxWidth))
            {
                double layoutWidth = cmd.MaxWidth;
                if (cmd.TextAlignment == TextAlignment.Center)
                    x += (layoutWidth - drawWidth) / 2.0;
                else
                    x += layoutWidth - drawWidth;
            }

            int index = 0;
            while (index < cmd.Text.Length)
            {
                int glyphLen = 1;
                if (char.IsHighSurrogate(cmd.Text[index]) &&
                    index + 1 < cmd.Text.Length &&
                    char.IsLowSurrogate(cmd.Text[index + 1]))
                {
                    glyphLen = 2;
                }

                string glyph = cmd.Text.Substring(index, glyphLen);
                Cairo.cairo_move_to(_cr, x, y);
                Cairo.cairo_show_text(_cr, glyph);
                x += cmd.GlyphUniformAdvance * glyphLen;
                index += glyphLen;
            }
            return;
        }

        bool finiteWidth = !float.IsPositiveInfinity(cmd.MaxWidth);
        bool wrap = finiteWidth && (cmd.TextFlags & TextFormatFlags.NoWrap) == 0;
        if (cmd.Text.IndexOfAny(['\r', '\n']) >= 0 || wrap)
        {
            Cairo.cairo_font_extents(_cr, out CairoFontExtents fontExtents);
            double lineHeight = Math.Max(1.0, fontExtents.height);
            double baselineY = cmd.P1.Y + Math.Max(0.0, fontExtents.ascent);
            var lines = BuildWrappedLines(cmd.Text, wrap ? cmd.MaxWidth : float.PositiveInfinity, MeasureCairoWidth);

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                double drawX = cmd.P1.X;
                if (cmd.TextAlignment != TextAlignment.Near && finiteWidth)
                {
                    double lineWidth = MeasureCairoWidth(line);
                    double layoutWidth = cmd.MaxWidth;
                    if (cmd.TextAlignment == TextAlignment.Center)
                        drawX += (layoutWidth - lineWidth) / 2.0;
                    else
                        drawX += layoutWidth - lineWidth;
                }

                Cairo.cairo_move_to(_cr, drawX, baselineY + i * lineHeight);
                Cairo.cairo_show_text(_cr, line);
            }
            return;
        }

        if (cmd.TextAlignment != TextAlignment.Near && !float.IsPositiveInfinity(cmd.MaxWidth))
        {
            Cairo.cairo_text_extents(_cr, cmd.Text, out CairoTextExtents extents);
            double layoutWidth = cmd.MaxWidth;
            if (cmd.TextAlignment == TextAlignment.Center)
                x += (layoutWidth - extents.width) / 2.0;
            else // Far
                x += layoutWidth - extents.width;
        }

        Cairo.cairo_move_to(_cr, x, y);
        Cairo.cairo_show_text(_cr, cmd.Text);
    }

    private unsafe void ExecuteDrawImage(in DrawCommand cmd)
    {
        if (!ImageManager.TryGetImageData(cmd.Image, out var imgData)) return;

        // Cairo expects premultiplied ARGB32. Convert from BGRA.
        int pixelCount = imgData.Width * imgData.Height;
        byte[] argbPixels = new byte[pixelCount * 4];

        for (int p = 0; p < pixelCount; p++)
        {
            int si = p * 4;
            byte b = imgData.BgraPixels[si];
            byte g = imgData.BgraPixels[si + 1];
            byte r = imgData.BgraPixels[si + 2];
            byte a = imgData.BgraPixels[si + 3];
            // Cairo ARGB32 is native-endian: on little-endian it's stored as BGRA in memory
            // with premultiplied alpha
            argbPixels[si] = (byte)(b * a / 255);
            argbPixels[si + 1] = (byte)(g * a / 255);
            argbPixels[si + 2] = (byte)(r * a / 255);
            argbPixels[si + 3] = a;
        }

        int stride = imgData.Width * 4;

        fixed (byte* pPixels = argbPixels)
        {
            nint imgSurface = Cairo.cairo_image_surface_create_for_data(
                (nint)pPixels, Cairo.CAIRO_FORMAT_ARGB32, imgData.Width, imgData.Height, stride);

            int srcX, srcY, srcW, srcH;
            if (cmd.HasSrcRect)
            {
                srcX = (int)cmd.SrcRect.X;
                srcY = (int)cmd.SrcRect.Y;
                srcW = (int)cmd.SrcRect.Width;
                srcH = (int)cmd.SrcRect.Height;
            }
            else
            {
                srcX = 0; srcY = 0;
                srcW = imgData.Width;
                srcH = imgData.Height;
            }

            double scaleX = cmd.Rect.Width / srcW;
            double scaleY = cmd.Rect.Height / srcH;

            Cairo.cairo_save(_cr);
            Cairo.cairo_translate(_cr, cmd.Rect.X, cmd.Rect.Y);
            Cairo.cairo_scale(_cr, scaleX, scaleY);
            Cairo.cairo_set_source_surface(_cr, imgSurface, -srcX, -srcY);
            nint pattern = Cairo.cairo_get_source(_cr);
            if (pattern != 0)
            {
                Cairo.cairo_pattern_set_filter(pattern, _imageFilter);
            }

            Cairo.cairo_rectangle(_cr, 0, 0, srcW, srcH);
            Cairo.cairo_clip(_cr);

            if (cmd.Opacity < 1f)
                Cairo.cairo_paint_with_alpha(_cr, cmd.Opacity);
            else
                Cairo.cairo_paint(_cr);

            Cairo.cairo_restore(_cr);
            Cairo.cairo_surface_destroy(imgSurface);
        }
    }

    private double MeasureCairoWidth(string text)
    {
        Cairo.cairo_text_extents(_cr, text, out CairoTextExtents extents);
        return extents.x_advance;
    }

    private static List<string> BuildWrappedLines(string text, float maxWidth, Func<string, double> measureWidth)
    {
        var lines = new List<string>();
        string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        string[] hardLines = normalized.Split('\n');
        bool finite = !float.IsPositiveInfinity(maxWidth);

        for (int h = 0; h < hardLines.Length; h++)
        {
            string hard = hardLines[h];
            if (!finite)
            {
                lines.Add(hard);
                continue;
            }

            if (hard.Length == 0)
            {
                lines.Add(string.Empty);
                continue;
            }

            string[] words = hard.Split(' ');
            string current = string.Empty;
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                string candidate = current.Length == 0 ? word : current + " " + word;
                if (current.Length == 0 || measureWidth(candidate) <= maxWidth)
                {
                    current = candidate;
                }
                else
                {
                    lines.Add(current);
                    current = word;
                }
            }
            lines.Add(current);
        }

        if (lines.Count == 0)
        {
            lines.Add(string.Empty);
        }

        return lines;
    }

    private void ExecuteDrawRoundedRectangle(in DrawCommand cmd)
    {
        BuildRoundedRectPath(cmd.Rect, cmd.CornerRadius);
        Cairo.cairo_set_source_rgba(_cr, cmd.Color.R, cmd.Color.G, cmd.Color.B, cmd.Color.A);
        Cairo.cairo_set_line_width(_cr, cmd.StrokeWidth);
        SetCairoDash(_cr, cmd.DashStyle, cmd.StrokeWidth);
        Cairo.cairo_stroke(_cr);
    }

    private void ExecuteFillRoundedRectangle(in DrawCommand cmd)
    {
        BuildRoundedRectPath(cmd.Rect, cmd.CornerRadius);
        Cairo.cairo_set_source_rgba(_cr, cmd.Color.R, cmd.Color.G, cmd.Color.B, cmd.Color.A);
        Cairo.cairo_fill(_cr);
    }

    private void BuildRoundedRectPath(RectF rect, float radius)
    {
        double x = rect.X;
        double y = rect.Y;
        double w = rect.Width;
        double h = rect.Height;
        double r = radius;

        Cairo.cairo_new_path(_cr);
        // Top-right arc
        Cairo.cairo_arc(_cr, x + w - r, y + r, r, -Math.PI / 2, 0);
        // Bottom-right arc
        Cairo.cairo_arc(_cr, x + w - r, y + h - r, r, 0, Math.PI / 2);
        // Bottom-left arc
        Cairo.cairo_arc(_cr, x + r, y + h - r, r, Math.PI / 2, Math.PI);
        // Top-left arc
        Cairo.cairo_arc(_cr, x + r, y + r, r, Math.PI, 3 * Math.PI / 2);
        Cairo.cairo_close_path(_cr);
    }

    public float GetFontHeight(FontSpec font)
    {
        if (_cr == 0) return 0f;

        SetCairoFont(_cr, font);
        Cairo.cairo_font_extents(_cr, out CairoFontExtents extents);
        return (float)(extents.ascent + extents.descent);
    }

    public bool TryCopyPixelsBgra(out int width, out int height, out byte[] bgraPixels)
    {
        width = _width;
        height = _height;
        if (_surface == 0 || _width <= 0 || _height <= 0)
        {
            width = 0;
            height = 0;
            bgraPixels = [];
            return false;
        }

        Cairo.cairo_surface_flush(_surface);
        nint data = Cairo.cairo_image_surface_get_data(_surface);
        int stride = Cairo.cairo_image_surface_get_stride(_surface);
        if (data == 0 || stride <= 0)
        {
            width = 0;
            height = 0;
            bgraPixels = [];
            return false;
        }

        int rowBytes = _width * 4;
        bgraPixels = new byte[rowBytes * _height];
        for (int y = 0; y < _height; y++)
        {
            nint srcRow = data + (y * stride);
            Marshal.Copy(srcRow, bgraPixels, y * rowBytes, rowBytes);
        }

        return true;
    }

    // --- Helpers ---

    private static void SetCairoFont(nint cr, FontSpec font)
    {
        int slant = font.Style == FontStyle.Italic
            ? Cairo.CAIRO_FONT_SLANT_ITALIC
            : Cairo.CAIRO_FONT_SLANT_NORMAL;

        int weight = font.Weight >= FontWeight.Bold
            ? Cairo.CAIRO_FONT_WEIGHT_BOLD
            : Cairo.CAIRO_FONT_WEIGHT_NORMAL;

        Cairo.cairo_select_font_face(cr, font.FamilyName, slant, weight);
        Cairo.cairo_set_font_size(cr, font.Size);
    }

    private static void SetCairoDash(nint cr, DashStyle style, float strokeWidth)
    {
        double sw = strokeWidth;
        switch (style)
        {
            case DashStyle.Dash:
                Cairo.cairo_set_dash(cr, [sw * 4, sw * 2], 2, 0);
                break;
            case DashStyle.Dot:
                Cairo.cairo_set_dash(cr, [sw, sw * 2], 2, 0);
                break;
            case DashStyle.DashDot:
                Cairo.cairo_set_dash(cr, [sw * 4, sw * 2, sw, sw * 2], 4, 0);
                break;
            case DashStyle.DashDotDot:
                Cairo.cairo_set_dash(cr, [sw * 4, sw * 2, sw, sw * 2, sw, sw * 2], 6, 0);
                break;
            default: // Solid
                Cairo.cairo_set_dash(cr, [], 0, 0);
                break;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_cr != 0)
        {
            Cairo.cairo_destroy(_cr);
            _cr = 0;
        }

        if (_surface != 0)
        {
            Cairo.cairo_surface_destroy(_surface);
            _surface = 0;
        }

        if (_xSurface != 0)
        {
            Cairo.cairo_surface_destroy(_xSurface);
            _xSurface = 0;
        }
    }
}

