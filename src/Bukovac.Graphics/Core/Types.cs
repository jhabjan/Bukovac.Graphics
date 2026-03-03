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

using System.Runtime.InteropServices;

namespace Bukovac.Graphics;

/// <summary>
/// Defines the style used to stroke lines and outlines.
/// </summary>
public sealed class Pen
{
    public ColorF Color { get; set; }
    public float Width { get; set; }
    public DashStyle DashStyle { get; set; }
    public StrokeRenderMode RenderMode { get; set; }

    public Pen(ColorF color, float width = 1f)
    {
        Color = color;
        Width = width;
        DashStyle = DashStyle.Solid;
        RenderMode = StrokeRenderMode.Default;
    }
}

/// <summary>
/// Defines the fill used for shapes and text.
/// </summary>
public class Brush
{
    public ColorF Color { get; set; }

    public Brush(ColorF color)
    {
        Color = color;
    }
}

/// <summary>
/// A solid-color brush.
/// </summary>
public sealed class SolidBrush : Brush
{
    public SolidBrush(ColorF color) : base(color) { }
}

/// <summary>
/// Dash styles for pen strokes.
/// </summary>
public enum DashStyle
{
    Solid = 0,
    Dash = 1,
    Dot = 2,
    DashDot = 3,
    DashDotDot = 4,
}

/// <summary>
/// Stroke renderer behavior hint for vector outlines.
/// </summary>
public enum StrokeRenderMode
{
    Default = 0,
    AlphaAccurate = 1,
}

/// <summary>
/// Specifies font parameters for text rendering.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct FontSpec
{
    public string FamilyName { get; }
    public float Size { get; }
    public FontWeight Weight { get; }
    public FontStyle Style { get; }

    public FontSpec(string familyName, float size, FontWeight weight = FontWeight.Normal, FontStyle style = FontStyle.Normal)
    {
        FamilyName = familyName;
        Size = size;
        Weight = weight;
        Style = style;
    }
}

public enum FontWeight
{
    Thin = 100,
    Light = 300,
    Normal = 400,
    Medium = 500,
    SemiBold = 600,
    Bold = 700,
    ExtraBold = 800,
    Black = 900,
}

public enum FontStyle
{
    Normal = 0,
    Italic = 1,
}

/// <summary>
/// Represents a handle to a loaded image/bitmap resource managed by the rasterizer.
/// </summary>
public readonly struct ImageHandle(int id)
{
    public readonly int Id = id;
    public bool IsValid => Id > 0;
    public static readonly ImageHandle Invalid = new(0);
}

/// <summary>
/// Represents a native OS window handle.
/// Display and Visual are used by Linux X11 and default to 0 on other platforms.
/// </summary>
public readonly struct NativeWindowHandle(string kind, nint handle, nint display = 0, nint visual = 0)
{
    public readonly string Kind = kind;
    public readonly nint Handle = handle;
    public readonly nint Display = display;
    public readonly nint Visual = visual;

    public static NativeWindowHandle Hwnd(nint hwnd) => new("HWND", hwnd);
    public static NativeWindowHandle X11(nint display, nint window, nint visual) => new("X11", window, display, visual);
    public static NativeWindowHandle NSView(nint nsView) => new("NSView", nsView);

    public bool IsValid => Handle != 0;
}

public enum InterpolationMode
{
    Default = 0,
    NearestNeighbor,
    Bilinear,
    HighQualityBilinear,
    Bicubic,
    HighQualityBicubic,
}

public enum SmoothingMode
{
    Default = 0,
    None,
    AntiAlias,
    HighQuality,
}

public enum PixelOffsetMode
{
    Default = 0,
    None,
    HighSpeed,
    HighQuality,
    Half,
}

public enum CompositingQuality
{
    Default = 0,
    HighSpeed,
    HighQuality,
    GammaCorrected,
    AssumeLinear,
}

/// <summary>
/// Horizontal text alignment within a layout rectangle.
/// </summary>
public enum TextAlignment
{
    Near = 0,
    Center = 1,
    Far = 2,
}

/// <summary>
/// Text renderer behavior hint. Rasterizers may ignore unsupported modes.
/// </summary>
public enum TextRenderMode
{
    Default = 0,
    AlphaAccurate = 1,
}

/// <summary>
/// Flags controlling text rendering and measurement behavior.
/// </summary>
[Flags]
public enum TextFormatFlags
{
    None = 0,
    NoPadding = 1,
    MeasureTrailingSpaces = 2,
    NoWrap = 4,
    NoPrefix = 8,
}
