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

namespace Bukovac.Graphics.Rasterizers.Windows.GDI
{
/// <summary>
/// GDI32.dll P/Invoke declarations. Source-generated via LibraryImport for NativeAOT.
/// </summary>
internal static partial class Gdi32
{
    private const string LibraryName = "gdi32.dll";

    [LibraryImport(LibraryName)]
    public static partial nint CreateCompatibleDC(nint hdc);

    [LibraryImport(LibraryName)]
    public static partial nint CreateCompatibleBitmap(nint hdc, int cx, int cy);

    [LibraryImport(LibraryName)]
    public static partial nint CreateDIBSection(nint hdc, ref BITMAPINFO pbmi, uint usage, out nint ppvBits, nint hSection, uint offset);

    [LibraryImport(LibraryName)]
    public static partial nint SelectObject(nint hdc, nint h);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteObject(nint ho);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteDC(nint hdc);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool BitBlt(nint hdc, int x, int y, int cx, int cy, nint hdcSrc, int x1, int y1, uint rop);

    [LibraryImport(LibraryName)]
    public static partial nint CreateSolidBrush(uint color);

    [LibraryImport(LibraryName)]
    public static partial nint CreatePen(int iStyle, int cWidth, uint color);

    [LibraryImport(LibraryName)]
    public static partial int SetBkMode(nint hdc, int mode);

    [LibraryImport(LibraryName)]
    public static partial uint SetTextColor(nint hdc, uint color);

    [LibraryImport(LibraryName)]
    public static partial uint SetBkColor(nint hdc, uint color);

    [LibraryImport(LibraryName, EntryPoint = "TextOutW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool TextOut(nint hdc, int x, int y, string lpString, int c);

    [LibraryImport(LibraryName, EntryPoint = "TextOutW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool TextOutRaw(nint hdc, int x, int y, char* lpString, int c);

    [LibraryImport(LibraryName, EntryPoint = "GetTextExtentPoint32W", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetTextExtentPoint32(nint hdc, string lpString, int c, out SIZE lpSize);

    [LibraryImport(LibraryName, EntryPoint = "GetTextExtentPoint32W")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool GetTextExtentPoint32Raw(nint hdc, char* lpString, int c, out SIZE lpSize);

    [LibraryImport(LibraryName, EntryPoint = "ExtTextOutW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool ExtTextOutRaw(
        nint hdc, int x, int y, uint options, nint lprc, char* lpString, uint c, int* lpDx);

    [LibraryImport(LibraryName, EntryPoint = "CreateFontW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint CreateFont(
        int cHeight, int cWidth, int cEscapement, int cOrientation,
        int cWeight, uint bItalic, uint bUnderline, uint bStrikeOut,
        uint iCharSet, uint iOutPrecision, uint iClipPrecision,
        uint iQuality, uint iPitchAndFamily, string pszFaceName);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool Rectangle(nint hdc, int left, int top, int right, int bottom);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool Ellipse(nint hdc, int left, int top, int right, int bottom);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RoundRect(nint hdc, int left, int top, int right, int bottom, int width, int height);

    [LibraryImport(LibraryName, EntryPoint = "GetTextMetricsW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetTextMetrics(nint hdc, out TEXTMETRIC lptm);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool MoveToEx(nint hdc, int x, int y, nint lppt);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool LineTo(nint hdc, int x, int y);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool BeginPath(nint hdc);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EndPath(nint hdc);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool FillPath(nint hdc);

    [LibraryImport(LibraryName)]
    public static partial int SaveDC(nint hdc);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RestoreDC(nint hdc, int nSavedDC);

    [LibraryImport(LibraryName)]
    public static partial int IntersectClipRect(nint hdc, int left, int top, int right, int bottom);

    [LibraryImport(LibraryName)]
    public static partial int SelectClipRgn(nint hdc, nint hrgn);

    [LibraryImport(LibraryName)]
    public static partial nint CreateRectRgn(int left, int top, int right, int bottom);

    [LibraryImport(LibraryName)]
    public static partial int ExtSelectClipRgn(nint hdc, nint hrgn, int mode);

    [LibraryImport(LibraryName)]
    public static partial int GetClipBox(nint hdc, out RECT lprc);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PtVisible(nint hdc, int x, int y);

    // Constants
    public const int TRANSPARENT = 1;
    public const uint SRCCOPY = 0x00CC0020;
    public const uint DIB_RGB_COLORS = 0;
    public const int PS_SOLID = 0;
    public const int PS_DASH = 1;
    public const int PS_DOT = 2;
    public const int PS_DASHDOT = 3;
    public const int PS_DASHDOTDOT = 4;
    public const uint DEFAULT_CHARSET = 1;
    public const uint OUT_TT_PRECIS = 4;
    public const uint CLIP_DEFAULT_PRECIS = 0;
    public const uint DEFAULT_QUALITY = 0;
    public const uint NONANTIALIASED_QUALITY = 3;
    public const uint ANTIALIASED_QUALITY = 4;
    public const uint CLEARTYPE_QUALITY = 5;
    public const uint DEFAULT_PITCH = 0;
    public const int FW_NORMAL = 400;
    public const int FW_BOLD = 700;

    public static nint GetStockObject(int i) => GetStockObjectInternal(i);

    [LibraryImport(LibraryName, EntryPoint = "GetStockObject")]
    private static partial nint GetStockObjectInternal(int i);

    public const int NULL_BRUSH = 5;
    public const int NULL_PEN = 8;

    // --- Transform support ---

    [LibraryImport(LibraryName)]
    public static partial int SetGraphicsMode(nint hdc, int iMode);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWorldTransform(nint hdc, ref XFORM lpxf);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetWorldTransform(nint hdc, out XFORM lpxf);

    public const int GM_COMPATIBLE = 1;
    public const int GM_ADVANCED = 2;
    public const int ERROR = 0;
    public const int NULLREGION = 1;
    public const int SIMPLEREGION = 2;
    public const int COMPLEXREGION = 3;
    public const int RGN_AND = 1;

    // --- Stretch mode ---

    [LibraryImport(LibraryName)]
    public static partial int SetStretchBltMode(nint hdc, int mode);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetBrushOrgEx(nint hdc, int x, int y, nint lppt);

    public const int COLORONCOLOR = 3;
    public const int HALFTONE = 4;

    // --- Image blitting ---

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool StretchBlt(
        nint hdcDest, int xDest, int yDest, int wDest, int hDest,
        nint hdcSrc, int xSrc, int ySrc, int wSrc, int hSrc, uint rop);

    [LibraryImport(LibraryName)]
    public static partial int GetDeviceCaps(nint hdc, int index);

    public const int LOGPIXELSX = 88;
    public const int LOGPIXELSY = 90;
}

/// <summary>
/// Msimg32.dll P/Invoke declarations for alpha blending.
/// </summary>
internal static partial class Msimg32
{
    private const string LibraryName = "msimg32.dll";

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AlphaBlend(
        nint hdcDest, int xoriginDest, int yoriginDest, int wDest, int hDest,
        nint hdcSrc, int xoriginSrc, int yoriginSrc, int wSrc, int hSrc,
        BLENDFUNCTION ftn);

    public const byte AC_SRC_OVER = 0x00;
    public const byte AC_SRC_ALPHA = 0x01;
}

/// <summary>
/// User32.dll P/Invoke declarations.
/// </summary>
internal static partial class User32
{
    private const string LibraryName = "user32.dll";

    [LibraryImport(LibraryName)]
    public static partial nint GetDC(nint hWnd);

    [LibraryImport(LibraryName)]
    public static partial int ReleaseDC(nint hWnd, nint hDC);

    [LibraryImport(LibraryName, EntryPoint = "InvalidateRect")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool InvalidateRect(nint hWnd, nint lpRect, [MarshalAs(UnmanagedType.Bool)] bool bErase);

    [LibraryImport(LibraryName)]
    public static partial int FillRect(nint hdc, ref RECT lprc, nint hbr);

    [LibraryImport(LibraryName, EntryPoint = "DrawTextW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int DrawText(nint hdc, string lpchText, int cchText, ref RECT lprc, uint format);

    public const uint DT_LEFT = 0x00000000;
    public const uint DT_CENTER = 0x00000001;
    public const uint DT_RIGHT = 0x00000002;
    public const uint DT_WORDBREAK = 0x00000010;
    public const uint DT_SINGLELINE = 0x00000020;
    public const uint DT_CALCRECT = 0x00000400;
    public const uint DT_NOPREFIX = 0x00000800;
}

// --- Interop structs ---

[StructLayout(LayoutKind.Sequential)]
internal struct BITMAPINFOHEADER
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
internal struct BITMAPINFO
{
    public BITMAPINFOHEADER bmiHeader;
}

[StructLayout(LayoutKind.Sequential)]
internal struct SIZE
{
    public int cx;
    public int cy;
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
internal struct XFORM
{
    public float eM11;
    public float eM12;
    public float eM21;
    public float eM22;
    public float eDx;
    public float eDy;
}

[StructLayout(LayoutKind.Sequential)]
internal struct BLENDFUNCTION
{
    public byte BlendOp;
    public byte BlendFlags;
    public byte SourceConstantAlpha;
    public byte AlphaFormat;
}

[StructLayout(LayoutKind.Sequential)]
internal struct TEXTMETRIC
{
    public int tmHeight;
    public int tmAscent;
    public int tmDescent;
    public int tmInternalLeading;
    public int tmExternalLeading;
    public int tmAveCharWidth;
    public int tmMaxCharWidth;
    public int tmWeight;
    public int tmOverhang;
    public int tmDigitizedAspectX;
    public int tmDigitizedAspectY;
    public ushort tmFirstChar;
    public ushort tmLastChar;
    public ushort tmDefaultChar;
    public ushort tmBreakChar;
    public byte tmItalic;
    public byte tmUnderlined;
    public byte tmStruckOut;
    public byte tmPitchAndFamily;
    public byte tmCharSet;
}
}
