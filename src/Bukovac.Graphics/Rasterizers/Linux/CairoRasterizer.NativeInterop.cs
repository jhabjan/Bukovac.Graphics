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

namespace Bukovac.Graphics.Rasterizers.Linux;

/// <summary>
/// Cairo library P/Invoke declarations. Source-generated via LibraryImport for NativeAOT.
/// </summary>
internal static partial class Cairo
{
    private const string LibraryName = "libcairo.so.2";

    // --- Surface creation (X11) ---

    [LibraryImport(LibraryName)]
    public static partial nint cairo_xlib_surface_create(nint display, nint drawable, nint visual, int width, int height);

    [LibraryImport(LibraryName)]
    public static partial void cairo_xlib_surface_set_size(nint surface, int width, int height);

    // --- Image surface (off-screen / bitmap mode) ---

    [LibraryImport(LibraryName)]
    public static partial nint cairo_image_surface_create(int format, int width, int height);

    [LibraryImport(LibraryName)]
    public static partial nint cairo_image_surface_create_for_data(
        nint data, int format, int width, int height, int stride);

    [LibraryImport(LibraryName)]
    public static partial nint cairo_image_surface_get_data(nint surface);

    [LibraryImport(LibraryName)]
    public static partial int cairo_image_surface_get_stride(nint surface);

    // --- Context ---

    [LibraryImport(LibraryName)]
    public static partial nint cairo_create(nint target);

    [LibraryImport(LibraryName)]
    public static partial void cairo_destroy(nint cr);

    [LibraryImport(LibraryName)]
    public static partial void cairo_surface_destroy(nint surface);

    [LibraryImport(LibraryName)]
    public static partial void cairo_surface_flush(nint surface);

    // --- State save/restore ---

    [LibraryImport(LibraryName)]
    public static partial void cairo_save(nint cr);

    [LibraryImport(LibraryName)]
    public static partial void cairo_restore(nint cr);

    // --- Color ---

    [LibraryImport(LibraryName)]
    public static partial void cairo_set_source_rgb(nint cr, double red, double green, double blue);

    [LibraryImport(LibraryName)]
    public static partial void cairo_set_source_rgba(nint cr, double red, double green, double blue, double alpha);

    // --- Path operations ---

    [LibraryImport(LibraryName)]
    public static partial void cairo_new_path(nint cr);

    [LibraryImport(LibraryName)]
    public static partial void cairo_move_to(nint cr, double x, double y);

    [LibraryImport(LibraryName)]
    public static partial void cairo_line_to(nint cr, double x, double y);

    [LibraryImport(LibraryName)]
    public static partial void cairo_rectangle(nint cr, double x, double y, double width, double height);

    [LibraryImport(LibraryName)]
    public static partial void cairo_arc(nint cr, double xc, double yc, double radius, double angle1, double angle2);

    [LibraryImport(LibraryName)]
    public static partial void cairo_close_path(nint cr);

    // --- Drawing ---

    [LibraryImport(LibraryName)]
    public static partial void cairo_fill(nint cr);

    [LibraryImport(LibraryName)]
    public static partial void cairo_stroke(nint cr);

    [LibraryImport(LibraryName)]
    public static partial void cairo_paint(nint cr);

    [LibraryImport(LibraryName)]
    public static partial void cairo_paint_with_alpha(nint cr, double alpha);

    [LibraryImport(LibraryName)]
    public static partial void cairo_set_operator(nint cr, int op);

    // --- Line style ---

    [LibraryImport(LibraryName)]
    public static partial void cairo_set_line_width(nint cr, double width);

    [LibraryImport(LibraryName)]
    public static partial void cairo_set_dash(nint cr, [MarshalAs(UnmanagedType.LPArray)] double[] dashes, int numDashes, double offset);

    // --- Clipping ---

    [LibraryImport(LibraryName)]
    public static partial void cairo_clip(nint cr);

    [LibraryImport(LibraryName)]
    public static partial void cairo_reset_clip(nint cr);

    // --- Transform ---

    [LibraryImport(LibraryName)]
    public static partial void cairo_set_matrix(nint cr, ref CairoMatrix matrix);

    [LibraryImport(LibraryName)]
    public static partial void cairo_identity_matrix(nint cr);

    [LibraryImport(LibraryName)]
    public static partial void cairo_translate(nint cr, double tx, double ty);

    [LibraryImport(LibraryName)]
    public static partial void cairo_scale(nint cr, double sx, double sy);

    // --- Text (toy API) ---

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial void cairo_select_font_face(nint cr, string family, int slant, int weight);

    [LibraryImport(LibraryName)]
    public static partial void cairo_set_font_size(nint cr, double size);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial void cairo_show_text(nint cr, string text);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial void cairo_text_extents(nint cr, string text, out CairoTextExtents extents);

    [LibraryImport(LibraryName)]
    public static partial void cairo_font_extents(nint cr, out CairoFontExtents extents);

    // --- Image source ---

    [LibraryImport(LibraryName)]
    public static partial void cairo_set_source_surface(nint cr, nint surface, double x, double y);

    [LibraryImport(LibraryName)]
    public static partial nint cairo_get_source(nint cr);

    [LibraryImport(LibraryName)]
    public static partial void cairo_pattern_set_filter(nint pattern, int filter);

    [LibraryImport(LibraryName)]
    public static partial void cairo_set_antialias(nint cr, int antialias);

    // --- Constants ---

    public const int CAIRO_FORMAT_ARGB32 = 0;
    public const int CAIRO_FORMAT_RGB24 = 1;

    public const int CAIRO_FONT_SLANT_NORMAL = 0;
    public const int CAIRO_FONT_SLANT_ITALIC = 1;

    public const int CAIRO_FONT_WEIGHT_NORMAL = 0;
    public const int CAIRO_FONT_WEIGHT_BOLD = 1;

    public const int CAIRO_FILTER_FAST = 0;
    public const int CAIRO_FILTER_GOOD = 1;
    public const int CAIRO_FILTER_BEST = 2;
    public const int CAIRO_FILTER_NEAREST = 3;
    public const int CAIRO_FILTER_BILINEAR = 4;

    public const int CAIRO_ANTIALIAS_DEFAULT = 0;
    public const int CAIRO_ANTIALIAS_NONE = 1;
    public const int CAIRO_ANTIALIAS_GRAY = 2;
    public const int CAIRO_ANTIALIAS_SUBPIXEL = 3;
    public const int CAIRO_ANTIALIAS_FAST = 4;
    public const int CAIRO_ANTIALIAS_GOOD = 5;
    public const int CAIRO_ANTIALIAS_BEST = 6;

    public const int CAIRO_OPERATOR_OVER = 2;
    public const int CAIRO_OPERATOR_SOURCE = 1;
}

// --- Interop structs ---

[StructLayout(LayoutKind.Sequential)]
internal struct CairoMatrix
{
    public double xx;
    public double yx;
    public double xy;
    public double yy;
    public double x0;
    public double y0;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CairoFontExtents
{
    public double ascent;
    public double descent;
    public double height;
    public double max_x_advance;
    public double max_y_advance;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CairoTextExtents
{
    public double x_bearing;
    public double y_bearing;
    public double width;
    public double height;
    public double x_advance;
    public double y_advance;
}
