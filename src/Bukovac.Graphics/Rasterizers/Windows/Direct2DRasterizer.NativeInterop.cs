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

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bukovac.Graphics.Rasterizers.Windows.Direct2D
{
// ── COM Helpers ──────────────────────────────────────────────────────────────

internal static unsafe class ComHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Release(nint ptr)
    {
        if (ptr == 0) return 0;
        var vtbl = *(nint**)ptr;
        var release = (delegate* unmanaged[Stdcall]<nint, uint>)vtbl[2];
        return release(ptr);
    }
}

// ── D2D1 Factory Creation ────────────────────────────────────────────────────

internal static partial class D2D1
{
    internal static readonly Guid IID_ID2D1Factory = new("06152247-6F50-465A-9245-118BFD3B6007");

    [LibraryImport("d2d1.dll")]
    internal static partial int D2D1CreateFactory(
        D2D1_FACTORY_TYPE factoryType,
        in Guid riid,
        nint pFactoryOptions,
        out nint ppIFactory);
}

// ── DirectWrite Factory Creation ─────────────────────────────────────────────

internal static partial class DWrite
{
    internal static readonly Guid IID_IDWriteFactory = new("B859EE5A-D838-4B5B-A2E8-1ADC7D93DB48");

    [LibraryImport("dwrite.dll")]
    internal static partial int DWriteCreateFactory(
        DWRITE_FACTORY_TYPE factoryType,
        in Guid iid,
        out nint factory);
}

// ── User32 ───────────────────────────────────────────────────────────────────

internal static partial class D2DUser32
{
    [LibraryImport("user32.dll")]
    public static partial nint GetDC(nint hWnd);

    [LibraryImport("user32.dll")]
    public static partial int ReleaseDC(nint hWnd, nint hDC);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetClientRect(nint hWnd, out RECT lpRect);
}

// ── GDI32 (for GetDeviceCaps) ────────────────────────────────────────────────

internal static partial class D2DGdi32
{
    [LibraryImport("gdi32.dll")]
    public static partial int GetDeviceCaps(nint hdc, int index);

    [LibraryImport("gdi32.dll")]
    public static partial nint CreateCompatibleDC(nint hdc);

    [LibraryImport("gdi32.dll")]
    public static partial nint CreateDIBSection(
        nint hdc, ref D2D_BITMAPINFO pbmi, uint usage, out nint ppvBits, nint hSection, uint offset);

    [LibraryImport("gdi32.dll")]
    public static partial nint SelectObject(nint hdc, nint h);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteObject(nint ho);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteDC(nint hdc);

    public const int LOGPIXELSY = 90;
    public const uint DIB_RGB_COLORS = 0;
}

// ── D2D1 Enums ───────────────────────────────────────────────────────────────

internal enum D2D1_FACTORY_TYPE : uint
{
    SINGLE_THREADED = 0,
    MULTI_THREADED = 1
}

internal enum D2D1_RENDER_TARGET_TYPE : uint
{
    DEFAULT = 0,
    SOFTWARE = 1,
    HARDWARE = 2
}

internal enum D2D1_ALPHA_MODE : uint
{
    UNKNOWN = 0,
    PREMULTIPLIED = 1,
    STRAIGHT = 2,
    IGNORE = 3
}

internal enum D2D1_PRESENT_OPTIONS : uint
{
    NONE = 0x00000000,
    RETAIN_CONTENTS = 0x00000001,
    IMMEDIATELY = 0x00000002
}

internal enum D2D1_ANTIALIAS_MODE : uint
{
    PER_PRIMITIVE = 0,
    ALIASED = 1
}

internal enum D2D1_TEXT_ANTIALIAS_MODE : uint
{
    DEFAULT = 0,
    CLEARTYPE = 1,
    GRAYSCALE = 2,
    ALIASED = 3
}

internal enum D2D1_DRAW_TEXT_OPTIONS : uint
{
    NONE = 0,
    NO_SNAP = 0x00000001,
    CLIP = 0x00000002,
    ENABLE_COLOR_FONT = 0x00000004,
}

internal enum D2D1_BITMAP_INTERPOLATION_MODE : uint
{
    NEAREST_NEIGHBOR = 0,
    LINEAR = 1
}

internal enum D2D1_DASH_STYLE : uint
{
    SOLID = 0,
    DASH = 1,
    DOT = 2,
    DASH_DOT = 3,
    DASH_DOT_DOT = 4,
    CUSTOM = 5,
}

internal enum D2D1_CAP_STYLE : uint
{
    FLAT = 0,
    SQUARE = 1,
    ROUND = 2,
    TRIANGLE = 3,
}

internal enum D2D1_LINE_JOIN : uint
{
    MITER = 0,
    BEVEL = 1,
    ROUND = 2,
    MITER_OR_BEVEL = 3,
}

// ── DirectWrite Enums ────────────────────────────────────────────────────────

internal enum DWRITE_FACTORY_TYPE : uint
{
    SHARED = 0,
    ISOLATED = 1
}

internal enum DWRITE_FONT_WEIGHT : uint
{
    THIN = 100,
    EXTRA_LIGHT = 200,
    LIGHT = 300,
    NORMAL = 400,
    MEDIUM = 500,
    SEMI_BOLD = 600,
    BOLD = 700,
    EXTRA_BOLD = 800,
    BLACK = 900
}

internal enum DWRITE_FONT_STYLE : uint
{
    NORMAL = 0,
    OBLIQUE = 1,
    ITALIC = 2
}

internal enum DWRITE_FONT_STRETCH : uint
{
    NORMAL = 5,
}

internal enum DWRITE_TEXT_ALIGNMENT : uint
{
    LEADING = 0,
    TRAILING = 1,
    CENTER = 2,
    JUSTIFIED = 3
}

internal enum DWRITE_PARAGRAPH_ALIGNMENT : uint
{
    NEAR = 0,
    FAR = 1,
    CENTER = 2
}

internal enum DWRITE_WORD_WRAPPING : uint
{
    WRAP = 0,
    NO_WRAP = 1,
}

// ── D2D1 Structs ─────────────────────────────────────────────────────────────

[StructLayout(LayoutKind.Sequential)]
internal readonly struct D2D1_COLOR_F(float r, float g, float b, float a)
{
    public readonly float r = r;
    public readonly float g = g;
    public readonly float b = b;
    public readonly float a = a;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct D2D1_POINT_2F(float x, float y)
{
    public readonly float x = x;
    public readonly float y = y;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct D2D1_RECT_F(float left, float top, float right, float bottom)
{
    public readonly float left = left;
    public readonly float top = top;
    public readonly float right = right;
    public readonly float bottom = bottom;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct D2D1_SIZE_U(uint width, uint height)
{
    public readonly uint width = width;
    public readonly uint height = height;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct D2D1_SIZE_F(float width, float height)
{
    public readonly float width = width;
    public readonly float height = height;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct D2D1_PIXEL_FORMAT(uint format, D2D1_ALPHA_MODE alphaMode)
{
    public readonly uint format = format;
    public readonly D2D1_ALPHA_MODE alphaMode = alphaMode;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct D2D1_BITMAP_PROPERTIES(D2D1_PIXEL_FORMAT pixelFormat, float dpiX, float dpiY)
{
    public readonly D2D1_PIXEL_FORMAT pixelFormat = pixelFormat;
    public readonly float dpiX = dpiX;
    public readonly float dpiY = dpiY;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct D2D1_RENDER_TARGET_PROPERTIES(
    D2D1_RENDER_TARGET_TYPE type,
    D2D1_PIXEL_FORMAT pixelFormat,
    float dpiX,
    float dpiY,
    uint usage,
    uint minLevel)
{
    public readonly D2D1_RENDER_TARGET_TYPE type = type;
    public readonly D2D1_PIXEL_FORMAT pixelFormat = pixelFormat;
    public readonly float dpiX = dpiX;
    public readonly float dpiY = dpiY;
    public readonly uint usage = usage;
    public readonly uint minLevel = minLevel;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct D2D1_HWND_RENDER_TARGET_PROPERTIES(nint hwnd, D2D1_SIZE_U pixelSize, D2D1_PRESENT_OPTIONS presentOptions)
{
    public readonly nint hwnd = hwnd;
    public readonly D2D1_SIZE_U pixelSize = pixelSize;
    public readonly D2D1_PRESENT_OPTIONS presentOptions = presentOptions;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct D2D1_ROUNDED_RECT(D2D1_RECT_F rect, float radiusX, float radiusY)
{
    public readonly D2D1_RECT_F rect = rect;
    public readonly float radiusX = radiusX;
    public readonly float radiusY = radiusY;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct D2D1_MATRIX_3X2_F(float m11, float m12, float m21, float m22, float dx, float dy)
{
    public readonly float m11 = m11;
    public readonly float m12 = m12;
    public readonly float m21 = m21;
    public readonly float m22 = m22;
    public readonly float dx = dx;
    public readonly float dy = dy;

    public static D2D1_MATRIX_3X2_F Identity => new(1, 0, 0, 1, 0, 0);
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct D2D1_ELLIPSE(D2D1_POINT_2F point, float radiusX, float radiusY)
{
    public readonly D2D1_POINT_2F point = point;
    public readonly float radiusX = radiusX;
    public readonly float radiusY = radiusY;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct D2D1_STROKE_STYLE_PROPERTIES(
    D2D1_CAP_STYLE startCap,
    D2D1_CAP_STYLE endCap,
    D2D1_CAP_STYLE dashCap,
    D2D1_LINE_JOIN lineJoin,
    float miterLimit,
    D2D1_DASH_STYLE dashStyle,
    float dashOffset)
{
    public readonly D2D1_CAP_STYLE startCap = startCap;
    public readonly D2D1_CAP_STYLE endCap = endCap;
    public readonly D2D1_CAP_STYLE dashCap = dashCap;
    public readonly D2D1_LINE_JOIN lineJoin = lineJoin;
    public readonly float miterLimit = miterLimit;
    public readonly D2D1_DASH_STYLE dashStyle = dashStyle;
    public readonly float dashOffset = dashOffset;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    public int left;
    public int top;
    public int right;
    public int bottom;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D_BITMAPINFOHEADER
{
    public uint biSize;
    public int biWidth;
    public int biHeight;
    public ushort biPlanes;
    public ushort biBitCount;
    public uint biCompression;
    public uint biSizeImage;
    public int biXPelsPerMeter;
    public int biYPelsPerMeter;
    public uint biClrUsed;
    public uint biClrImportant;
}

[StructLayout(LayoutKind.Sequential)]
internal struct D2D_BITMAPINFO
{
    public D2D_BITMAPINFOHEADER bmiHeader;
}

// ── DirectWrite Structs ──────────────────────────────────────────────────────

[StructLayout(LayoutKind.Sequential)]
internal readonly struct DWRITE_TEXT_METRICS(
    float left,
    float top,
    float width,
    float widthIncludingTrailingWhitespace,
    float height,
    float layoutWidth,
    float layoutHeight,
    uint maxBidiReorderingDepth,
    uint lineCount)
{
    public readonly float left = left;
    public readonly float top = top;
    public readonly float width = width;
    public readonly float widthIncludingTrailingWhitespace = widthIncludingTrailingWhitespace;
    public readonly float height = height;
    public readonly float layoutWidth = layoutWidth;
    public readonly float layoutHeight = layoutHeight;
    public readonly uint maxBidiReorderingDepth = maxBidiReorderingDepth;
    public readonly uint lineCount = lineCount;
}

// ── COM VTable Wrappers ──────────────────────────────────────────────────────

#pragma warning disable CS0649

internal unsafe struct ID2D1Factory { public void** lpVtbl; }
internal unsafe struct ID2D1RenderTarget { public void** lpVtbl; }
internal unsafe struct ID2D1Bitmap { public void** lpVtbl; }
internal unsafe struct ID2D1DCRenderTarget { public void** lpVtbl; }
internal unsafe struct IDWriteFactory { public void** lpVtbl; }

#pragma warning restore CS0649

// ── D2D1 VTable Methods ─────────────────────────────────────────────────────

internal static unsafe class D2D1VTable
{
    // ID2D1Factory vtbl indices
    private const int CreateHwndRenderTargetIndex = 14;
    private const int CreateDcRenderTargetIndex = 16;
    private const int BindDCIndex = 57;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CreateHwndRenderTarget(
        ID2D1Factory* factory,
        ref D2D1_RENDER_TARGET_PROPERTIES rtProps,
        ref D2D1_HWND_RENDER_TARGET_PROPERTIES hwndProps,
        out nint renderTarget)
    {
        nint rt = 0;
        fixed (D2D1_RENDER_TARGET_PROPERTIES* pRt = &rtProps)
        fixed (D2D1_HWND_RENDER_TARGET_PROPERTIES* pHwnd = &hwndProps)
        {
            var fn = (delegate* unmanaged[Stdcall]<ID2D1Factory*, D2D1_RENDER_TARGET_PROPERTIES*, D2D1_HWND_RENDER_TARGET_PROPERTIES*, nint*, int>)(factory->lpVtbl[CreateHwndRenderTargetIndex]);
            int hr = fn(factory, pRt, pHwnd, &rt);
            renderTarget = rt;
            return hr;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CreateDcRenderTarget(
        ID2D1Factory* factory,
        ref D2D1_RENDER_TARGET_PROPERTIES rtProps,
        out nint dcRenderTarget)
    {
        nint rt = 0;
        fixed (D2D1_RENDER_TARGET_PROPERTIES* pRt = &rtProps)
        {
            var fn = (delegate* unmanaged[Stdcall]<ID2D1Factory*, D2D1_RENDER_TARGET_PROPERTIES*, nint*, int>)(factory->lpVtbl[CreateDcRenderTargetIndex]);
            int hr = fn(factory, pRt, &rt);
            dcRenderTarget = rt;
            return hr;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BindDC(ID2D1DCRenderTarget* dcRt, nint hdc, ref RECT rect)
    {
        fixed (RECT* pRect = &rect)
        {
            var fn = (delegate* unmanaged[Stdcall]<ID2D1DCRenderTarget*, nint, RECT*, int>)(dcRt->lpVtbl[BindDCIndex]);
            return fn(dcRt, hdc, pRect);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BeginDraw(ID2D1RenderTarget* rt)
    {
        var fn = (delegate* unmanaged[Stdcall]<ID2D1RenderTarget*, void>)(rt->lpVtbl[48]);
        fn(rt);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EndDraw(ID2D1RenderTarget* rt)
    {
        ulong tag1 = 0, tag2 = 0;
        var fn = (delegate* unmanaged[Stdcall]<ID2D1RenderTarget*, ulong*, ulong*, int>)(rt->lpVtbl[49]);
        return fn(rt, &tag1, &tag2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear(ID2D1RenderTarget* rt, in D2D1_COLOR_F color)
    {
        fixed (D2D1_COLOR_F* p = &color)
        {
            var fn = (delegate* unmanaged[Stdcall]<ID2D1RenderTarget*, D2D1_COLOR_F*, void>)(rt->lpVtbl[47]);
            fn(rt, p);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CreateSolidColorBrush(ID2D1RenderTarget* rt, in D2D1_COLOR_F color, out nint brush)
    {
        nint b = 0;
        fixed (D2D1_COLOR_F* pColor = &color)
        {
            var fn = (delegate* unmanaged[Stdcall]<ID2D1RenderTarget*, D2D1_COLOR_F*, nint, nint*, int>)(rt->lpVtbl[8]);
            int hr = fn(rt, pColor, 0, &b);
            brush = b;
            return hr;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawLine(ID2D1RenderTarget* rt, D2D1_POINT_2F p0, D2D1_POINT_2F p1, nint brush, float strokeWidth, nint strokeStyle)
    {
        var fn = (delegate* unmanaged[Stdcall]<ID2D1RenderTarget*, D2D1_POINT_2F, D2D1_POINT_2F, nint, float, nint, void>)(rt->lpVtbl[15]);
        fn(rt, p0, p1, brush, strokeWidth, strokeStyle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawRectangle(ID2D1RenderTarget* rt, in D2D1_RECT_F rect, nint brush, float strokeWidth, nint strokeStyle)
    {
        fixed (D2D1_RECT_F* pRect = &rect)
        {
            var fn = (delegate* unmanaged[Stdcall]<ID2D1RenderTarget*, D2D1_RECT_F*, nint, float, nint, void>)(rt->lpVtbl[16]);
            fn(rt, pRect, brush, strokeWidth, strokeStyle);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FillRectangle(ID2D1RenderTarget* rt, in D2D1_RECT_F rect, nint brush)
    {
        fixed (D2D1_RECT_F* pRect = &rect)
        {
            var fn = (delegate* unmanaged[Stdcall]<ID2D1RenderTarget*, D2D1_RECT_F*, nint, void>)(rt->lpVtbl[17]);
            fn(rt, pRect, brush);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawRoundedRectangle(ID2D1RenderTarget* rt, in D2D1_ROUNDED_RECT rect, nint brush, float strokeWidth, nint strokeStyle)
    {
        fixed (D2D1_ROUNDED_RECT* pRect = &rect)
        {
            var fn = (delegate* unmanaged[Stdcall]<ID2D1RenderTarget*, D2D1_ROUNDED_RECT*, nint, float, nint, void>)(rt->lpVtbl[18]);
            fn(rt, pRect, brush, strokeWidth, strokeStyle);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FillRoundedRectangle(ID2D1RenderTarget* rt, in D2D1_ROUNDED_RECT rect, nint brush)
    {
        fixed (D2D1_ROUNDED_RECT* pRect = &rect)
        {
            var fn = (delegate* unmanaged[Stdcall]<ID2D1RenderTarget*, D2D1_ROUNDED_RECT*, nint, void>)(rt->lpVtbl[19]);
            fn(rt, pRect, brush);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawEllipse(ID2D1RenderTarget* rt, in D2D1_ELLIPSE ellipse, nint brush, float strokeWidth, nint strokeStyle)
    {
        fixed (D2D1_ELLIPSE* pEllipse = &ellipse)
        {
            var fn = (delegate* unmanaged[Stdcall]<ID2D1RenderTarget*, D2D1_ELLIPSE*, nint, float, nint, void>)(rt->lpVtbl[20]);
            fn(rt, pEllipse, brush, strokeWidth, strokeStyle);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FillEllipse(ID2D1RenderTarget* rt, in D2D1_ELLIPSE ellipse, nint brush)
    {
        fixed (D2D1_ELLIPSE* pEllipse = &ellipse)
        {
            var fn = (delegate* unmanaged[Stdcall]<ID2D1RenderTarget*, D2D1_ELLIPSE*, nint, void>)(rt->lpVtbl[21]);
            fn(rt, pEllipse, brush);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PushAxisAlignedClip(ID2D1RenderTarget* rt, in D2D1_RECT_F rect)
    {
        fixed (D2D1_RECT_F* pRect = &rect)
        {
            var fn = (delegate* unmanaged[Stdcall]<ID2D1RenderTarget*, D2D1_RECT_F*, D2D1_ANTIALIAS_MODE, void>)(rt->lpVtbl[45]);
            fn(rt, pRect, D2D1_ANTIALIAS_MODE.PER_PRIMITIVE);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PopAxisAlignedClip(ID2D1RenderTarget* rt)
    {
        var fn = (delegate* unmanaged[Stdcall]<ID2D1RenderTarget*, void>)(rt->lpVtbl[46]);
        fn(rt);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetTransform(ID2D1RenderTarget* rt, in D2D1_MATRIX_3X2_F matrix)
    {
        fixed (D2D1_MATRIX_3X2_F* pMatrix = &matrix)
        {
            var fn = (delegate* unmanaged[Stdcall]<ID2D1RenderTarget*, D2D1_MATRIX_3X2_F*, void>)(rt->lpVtbl[30]);
            fn(rt, pMatrix);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static D2D1_MATRIX_3X2_F GetTransform(ID2D1RenderTarget* rt)
    {
        D2D1_MATRIX_3X2_F matrix = default;
        var fn = (delegate* unmanaged[Stdcall]<ID2D1RenderTarget*, D2D1_MATRIX_3X2_F*, void>)(rt->lpVtbl[31]);
        fn(rt, &matrix);
        return matrix;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetAntialiasMode(ID2D1RenderTarget* rt, D2D1_ANTIALIAS_MODE mode)
    {
        var fn = (delegate* unmanaged[Stdcall]<ID2D1RenderTarget*, D2D1_ANTIALIAS_MODE, void>)(rt->lpVtbl[32]);
        fn(rt, mode);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetTextAntialiasMode(ID2D1RenderTarget* rt, D2D1_TEXT_ANTIALIAS_MODE mode)
    {
        var fn = (delegate* unmanaged[Stdcall]<ID2D1RenderTarget*, D2D1_TEXT_ANTIALIAS_MODE, void>)(rt->lpVtbl[34]);
        fn(rt, mode);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawText(ID2D1RenderTarget* rt, ReadOnlySpan<char> text, nint textFormat, in D2D1_RECT_F layoutRect, nint brush, D2D1_DRAW_TEXT_OPTIONS options)
    {
        if (text.IsEmpty) return;
        fixed (char* pText = text)
        fixed (D2D1_RECT_F* pRect = &layoutRect)
        {
            var fn = (delegate* unmanaged[Stdcall]<ID2D1RenderTarget*, char*, uint, nint, D2D1_RECT_F*, nint, D2D1_DRAW_TEXT_OPTIONS, uint, void>)(rt->lpVtbl[27]);
            fn(rt, pText, (uint)text.Length, textFormat, pRect, brush, options, 0);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CreateBitmap(
        ID2D1RenderTarget* rt,
        D2D1_SIZE_U size,
        nint srcData,
        uint pitch,
        in D2D1_BITMAP_PROPERTIES props,
        out nint bitmap)
    {
        nint bmp = 0;
        fixed (D2D1_BITMAP_PROPERTIES* pProps = &props)
        {
            var fn = (delegate* unmanaged[Stdcall]<ID2D1RenderTarget*, D2D1_SIZE_U, nint, uint, D2D1_BITMAP_PROPERTIES*, nint*, int>)(rt->lpVtbl[4]);
            int hr = fn(rt, size, srcData, pitch, pProps, &bmp);
            bitmap = bmp;
            return hr;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawBitmap(
        ID2D1RenderTarget* rt,
        nint bitmap,
        in D2D1_RECT_F destRect,
        float opacity,
        D2D1_BITMAP_INTERPOLATION_MODE interpolationMode,
        in D2D1_RECT_F srcRect)
    {
        fixed (D2D1_RECT_F* pDest = &destRect)
        fixed (D2D1_RECT_F* pSrc = &srcRect)
        {
            var fn = (delegate* unmanaged[Stdcall]<ID2D1RenderTarget*, nint, D2D1_RECT_F*, float, D2D1_BITMAP_INTERPOLATION_MODE, D2D1_RECT_F*, void>)(rt->lpVtbl[26]);
            fn(rt, bitmap, pDest, opacity, interpolationMode, pSrc);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CreateStrokeStyle(
        ID2D1Factory* factory,
        in D2D1_STROKE_STYLE_PROPERTIES properties,
        out nint strokeStyle)
    {
        nint s = 0;
        fixed (D2D1_STROKE_STYLE_PROPERTIES* pProps = &properties)
        {
            var fn = (delegate* unmanaged[Stdcall]<ID2D1Factory*, D2D1_STROKE_STYLE_PROPERTIES*, float*, uint, nint*, int>)(factory->lpVtbl[11]);
            int hr = fn(factory, pProps, null, 0, &s);
            strokeStyle = s;
            return hr;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CreateStrokeStyle(
        ID2D1Factory* factory,
        in D2D1_STROKE_STYLE_PROPERTIES properties,
        float* dashes,
        uint dashesCount,
        out nint strokeStyle)
    {
        nint s = 0;
        fixed (D2D1_STROKE_STYLE_PROPERTIES* pProps = &properties)
        {
            var fn = (delegate* unmanaged[Stdcall]<ID2D1Factory*, D2D1_STROKE_STYLE_PROPERTIES*, float*, uint, nint*, int>)(factory->lpVtbl[11]);
            int hr = fn(factory, pProps, dashes, dashesCount, &s);
            strokeStyle = s;
            return hr;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Resize(ID2D1RenderTarget* rt, D2D1_SIZE_U pixelSize)
    {
        // ID2D1HwndRenderTarget::Resize — vtbl index 58
        // (57 = CheckWindowState, 58 = Resize, 59 = GetHwnd)
        var fn = (delegate* unmanaged[Stdcall]<ID2D1RenderTarget*, D2D1_SIZE_U*, int>)(rt->lpVtbl[58]);
        D2D1_SIZE_U size = pixelSize;
        fn(rt, &size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetDpi(ID2D1RenderTarget* rt, float dpiX, float dpiY)
    {
        var fn = (delegate* unmanaged[Stdcall]<ID2D1RenderTarget*, float, float, void>)(rt->lpVtbl[51]);
        fn(rt, dpiX, dpiY);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetDpi(ID2D1RenderTarget* rt, out float dpiX, out float dpiY)
    {
        float x = 0f;
        float y = 0f;
        var fn = (delegate* unmanaged[Stdcall]<ID2D1RenderTarget*, float*, float*, void>)(rt->lpVtbl[52]);
        fn(rt, &x, &y);
        dpiX = x;
        dpiY = y;
    }
}

// ── DirectWrite VTable Methods ───────────────────────────────────────────────

internal static unsafe class DWriteVTable
{
    private const uint CreateTextFormatIndex = 15;
    private const uint CreateTextLayoutIndex = 18;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CreateTextFormat(
        IDWriteFactory* factory,
        string family,
        DWRITE_FONT_WEIGHT weight,
        DWRITE_FONT_STYLE style,
        float size,
        out nint textFormat)
    {
        nint format = 0;
        const string locale = "en-us";
        fixed (char* pFamily = family)
        fixed (char* pLocale = locale)
        {
            var fn = (delegate* unmanaged[Stdcall]<IDWriteFactory*, char*, nint, DWRITE_FONT_WEIGHT, DWRITE_FONT_STYLE, DWRITE_FONT_STRETCH, float, char*, nint*, int>)factory->lpVtbl[CreateTextFormatIndex];
            int hr = fn(factory, pFamily, 0, weight, style, DWRITE_FONT_STRETCH.NORMAL, size, pLocale, &format);
            textFormat = format;
            return hr;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CreateTextLayout(
        IDWriteFactory* factory,
        ReadOnlySpan<char> text,
        nint textFormat,
        float maxWidth,
        float maxHeight,
        out nint textLayout)
    {
        nint layout = 0;
        fixed (char* pText = text)
        {
            var fn = (delegate* unmanaged[Stdcall]<IDWriteFactory*, char*, uint, nint, float, float, nint*, int>)factory->lpVtbl[CreateTextLayoutIndex];
            int hr = fn(factory, pText, (uint)text.Length, textFormat, maxWidth, maxHeight, &layout);
            textLayout = layout;
            return hr;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SetTextAlignment(nint textFormat, DWRITE_TEXT_ALIGNMENT alignment)
    {
        var vtbl = *(nint**)textFormat;
        var fn = (delegate* unmanaged[Stdcall]<nint, DWRITE_TEXT_ALIGNMENT, int>)vtbl[3];
        return fn(textFormat, alignment);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SetParagraphAlignment(nint textFormat, DWRITE_PARAGRAPH_ALIGNMENT alignment)
    {
        var vtbl = *(nint**)textFormat;
        var fn = (delegate* unmanaged[Stdcall]<nint, DWRITE_PARAGRAPH_ALIGNMENT, int>)vtbl[4];
        return fn(textFormat, alignment);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SetWordWrapping(nint textFormat, DWRITE_WORD_WRAPPING wrapping)
    {
        var vtbl = *(nint**)textFormat;
        var fn = (delegate* unmanaged[Stdcall]<nint, DWRITE_WORD_WRAPPING, int>)vtbl[5];
        return fn(textFormat, wrapping);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetMetrics(nint textLayout, out DWRITE_TEXT_METRICS metrics)
    {
        metrics = default;
        var vtbl = *(nint**)textLayout;
        var fn = (delegate* unmanaged[Stdcall]<nint, DWRITE_TEXT_METRICS*, int>)vtbl[60];
        fixed (DWRITE_TEXT_METRICS* p = &metrics)
        {
            return fn(textLayout, p);
        }
    }
}
}