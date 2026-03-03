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

namespace Bukovac.Graphics.Commands;

/// <summary>
/// Identifies the type of drawing command.
/// </summary>
public enum DrawCommandKind : byte
{
    Clear,
    Save,
    Restore,
    SetTransform,
    ResetTransform,
    SetClip,
    ResetClip,
    DrawLine,
    DrawRectangle,
    FillRectangle,
    DrawEllipse,
    FillEllipse,
    DrawString,
    MeasureString,
    DrawImage,
    DrawRoundedRectangle,
    FillRoundedRectangle,
}

/// <summary>
/// A recorded drawing command. Uses a tagged-union style with shared fields.
/// </summary>
public struct DrawCommand
{
    public DrawCommandKind Kind;

    // Shared geometry
    public PointF P1;
    public PointF P2;
    public RectF Rect;
    public RectF SrcRect;
    public bool HasSrcRect;

    // Style
    public ColorF Color;
    public float StrokeWidth;
    public DashStyle DashStyle;
    public StrokeRenderMode StrokeRenderMode;
    public float Opacity;

    // Transform
    public Matrix3x2 Transform;

    // Text
    public string? Text;
    public int[]? GlyphAdvances;
    public int GlyphUniformAdvance;
    public FontSpec Font;
    public float MaxWidth;

    // Rounded rect
    public float CornerRadius;

    // Text layout
    public TextAlignment TextAlignment;
    public TextFormatFlags TextFlags;
    public TextRenderMode TextRenderMode;

    // Image
    public ImageHandle Image;

    // --- Factory methods ---

    public static DrawCommand Clear(ColorF color) => new()
    {
        Kind = DrawCommandKind.Clear,
        Color = color,
    };

    public static DrawCommand SaveState() => new()
    {
        Kind = DrawCommandKind.Save,
    };

    public static DrawCommand RestoreState() => new()
    {
        Kind = DrawCommandKind.Restore,
    };

    public static DrawCommand SetTransformCmd(Matrix3x2 transform) => new()
    {
        Kind = DrawCommandKind.SetTransform,
        Transform = transform,
    };

    public static DrawCommand ResetTransformCmd() => new()
    {
        Kind = DrawCommandKind.ResetTransform,
    };

    public static DrawCommand SetClipCmd(RectF rect) => new()
    {
        Kind = DrawCommandKind.SetClip,
        Rect = rect,
    };

    public static DrawCommand ResetClipCmd() => new()
    {
        Kind = DrawCommandKind.ResetClip,
    };

    public static DrawCommand Line(Pen pen, PointF p1, PointF p2) => new()
    {
        Kind = DrawCommandKind.DrawLine,
        P1 = p1,
        P2 = p2,
        Color = pen.Color,
        StrokeWidth = pen.Width,
        DashStyle = pen.DashStyle,
        StrokeRenderMode = pen.RenderMode,
    };

    public static DrawCommand DrawRect(Pen pen, RectF rect) => new()
    {
        Kind = DrawCommandKind.DrawRectangle,
        Rect = rect,
        Color = pen.Color,
        StrokeWidth = pen.Width,
        DashStyle = pen.DashStyle,
        StrokeRenderMode = pen.RenderMode,
    };

    public static DrawCommand FillRect(Brush brush, RectF rect) => new()
    {
        Kind = DrawCommandKind.FillRectangle,
        Rect = rect,
        Color = brush.Color,
    };

    public static DrawCommand DrawEllipseCmd(Pen pen, RectF rect) => new()
    {
        Kind = DrawCommandKind.DrawEllipse,
        Rect = rect,
        Color = pen.Color,
        StrokeWidth = pen.Width,
        DashStyle = pen.DashStyle,
        StrokeRenderMode = pen.RenderMode,
    };

    public static DrawCommand FillEllipseCmd(Brush brush, RectF rect) => new()
    {
        Kind = DrawCommandKind.FillEllipse,
        Rect = rect,
        Color = brush.Color,
    };

    public static DrawCommand String(string text, FontSpec font, Brush brush, PointF origin, float maxWidth, TextAlignment alignment = TextAlignment.Near, TextFormatFlags flags = TextFormatFlags.None, TextRenderMode renderMode = TextRenderMode.Default) => new()
    {
        Kind = DrawCommandKind.DrawString,
        Text = text,
        GlyphAdvances = null,
        GlyphUniformAdvance = 0,
        Font = font,
        Color = brush.Color,
        P1 = origin,
        MaxWidth = maxWidth,
        TextAlignment = alignment,
        TextFlags = flags,
        TextRenderMode = renderMode,
    };

    public static DrawCommand StringWithAdvances(string text, int[] glyphAdvances, FontSpec font, Brush brush, PointF origin, float maxWidth, TextAlignment alignment = TextAlignment.Near, TextFormatFlags flags = TextFormatFlags.None, TextRenderMode renderMode = TextRenderMode.Default) => new()
    {
        Kind = DrawCommandKind.DrawString,
        Text = text,
        GlyphAdvances = glyphAdvances,
        GlyphUniformAdvance = 0,
        Font = font,
        Color = brush.Color,
        P1 = origin,
        MaxWidth = maxWidth,
        TextAlignment = alignment,
        TextFlags = flags,
        TextRenderMode = renderMode,
    };

    public static DrawCommand StringWithUniformAdvance(string text, int glyphUniformAdvance, FontSpec font, Brush brush, PointF origin, float maxWidth, TextAlignment alignment = TextAlignment.Near, TextFormatFlags flags = TextFormatFlags.None, TextRenderMode renderMode = TextRenderMode.Default) => new()
    {
        Kind = DrawCommandKind.DrawString,
        Text = text,
        GlyphAdvances = null,
        GlyphUniformAdvance = glyphUniformAdvance,
        Font = font,
        Color = brush.Color,
        P1 = origin,
        MaxWidth = maxWidth,
        TextAlignment = alignment,
        TextFlags = flags,
        TextRenderMode = renderMode,
    };

    public static DrawCommand DrawRoundedRect(Pen pen, RectF rect, float cornerRadius) => new()
    {
        Kind = DrawCommandKind.DrawRoundedRectangle,
        Rect = rect,
        Color = pen.Color,
        StrokeWidth = pen.Width,
        DashStyle = pen.DashStyle,
        StrokeRenderMode = pen.RenderMode,
        CornerRadius = cornerRadius,
    };

    public static DrawCommand FillRoundedRect(Brush brush, RectF rect, float cornerRadius) => new()
    {
        Kind = DrawCommandKind.FillRoundedRectangle,
        Rect = rect,
        Color = brush.Color,
        CornerRadius = cornerRadius,
    };

    public static DrawCommand DrawImageCmd(ImageHandle image, RectF dest, RectF? src, float opacity) => new()
    {
        Kind = DrawCommandKind.DrawImage,
        Image = image,
        Rect = dest,
        SrcRect = src ?? RectF.Empty,
        HasSrcRect = src.HasValue,
        Opacity = opacity,
    };
}
