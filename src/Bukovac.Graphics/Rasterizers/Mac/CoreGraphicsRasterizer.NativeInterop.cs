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

namespace Bukovac.Graphics.Rasterizers.Mac;

/// <summary>
/// CoreGraphics framework P/Invoke declarations.
/// </summary>
internal static partial class CG
{
    private const string LibraryName = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    [LibraryImport(LibraryName)]
    public static partial nint CGBitmapContextCreate(
        nint data,
        nuint width,
        nuint height,
        nuint bitsPerComponent,
        nuint bytesPerRow,
        nint space,
        uint bitmapInfo);

    [LibraryImport(LibraryName)]
    public static partial nint CGBitmapContextCreateImage(nint context);

    [LibraryImport(LibraryName)]
    public static partial nint CGBitmapContextGetData(nint context);

    [LibraryImport(LibraryName)]
    public static partial nint CGColorSpaceCreateDeviceRGB();

    [LibraryImport(LibraryName)]
    public static partial void CGColorSpaceRelease(nint space);

    [LibraryImport(LibraryName)]
    public static partial void CGContextRelease(nint c);

    [LibraryImport(LibraryName)]
    public static partial void CGContextSaveGState(nint c);

    [LibraryImport(LibraryName)]
    public static partial void CGContextRestoreGState(nint c);

    [LibraryImport(LibraryName)]
    public static partial void CGContextSetRGBFillColor(nint c, double red, double green, double blue, double alpha);

    [LibraryImport(LibraryName)]
    public static partial void CGContextSetRGBStrokeColor(nint c, double red, double green, double blue, double alpha);

    [LibraryImport(LibraryName)]
    public static partial void CGContextFillRect(nint c, CGRect rect);

    [LibraryImport(LibraryName)]
    public static partial void CGContextStrokeRect(nint c, CGRect rect);

    [LibraryImport(LibraryName)]
    public static partial void CGContextStrokeRectWithWidth(nint c, CGRect rect, double width);

    [LibraryImport(LibraryName)]
    public static partial void CGContextClearRect(nint c, CGRect rect);

    [LibraryImport(LibraryName)]
    public static partial void CGContextFillEllipseInRect(nint c, CGRect rect);

    [LibraryImport(LibraryName)]
    public static partial void CGContextStrokeEllipseInRect(nint c, CGRect rect);

    [LibraryImport(LibraryName)]
    public static partial void CGContextSetLineWidth(nint c, double width);

    [LibraryImport(LibraryName)]
    public static partial void CGContextSetLineDash(nint c, double phase, [MarshalAs(UnmanagedType.LPArray)] double[] lengths, nuint count);

    [LibraryImport(LibraryName)]
    public static partial void CGContextMoveToPoint(nint c, double x, double y);

    [LibraryImport(LibraryName)]
    public static partial void CGContextAddLineToPoint(nint c, double x, double y);

    [LibraryImport(LibraryName)]
    public static partial void CGContextStrokePath(nint c);

    [LibraryImport(LibraryName)]
    public static partial void CGContextFillPath(nint c);

    [LibraryImport(LibraryName)]
    public static partial void CGContextBeginPath(nint c);

    [LibraryImport(LibraryName)]
    public static partial void CGContextClosePath(nint c);

    [LibraryImport(LibraryName)]
    public static partial void CGContextAddArc(nint c, double x, double y, double radius, double startAngle, double endAngle, int clockwise);

    [LibraryImport(LibraryName)]
    public static partial void CGContextAddRect(nint c, CGRect rect);

    [LibraryImport(LibraryName)]
    public static partial void CGContextClipToRect(nint c, CGRect rect);

    [LibraryImport(LibraryName)]
    public static partial void CGContextConcatCTM(nint c, CGAffineTransform transform);

    [LibraryImport(LibraryName)]
    public static partial CGAffineTransform CGContextGetCTM(nint c);

    [LibraryImport(LibraryName)]
    public static partial void CGContextSetAlpha(nint c, double alpha);

    [LibraryImport(LibraryName)]
    public static partial void CGContextSetInterpolationQuality(nint c, int quality);

    [LibraryImport(LibraryName)]
    public static partial void CGContextSetShouldAntialias(nint c, [MarshalAs(UnmanagedType.Bool)] bool shouldAntialias);

    [LibraryImport(LibraryName)]
    public static partial void CGContextSetAllowsAntialiasing(nint c, [MarshalAs(UnmanagedType.Bool)] bool allowsAntialiasing);

    [LibraryImport(LibraryName)]
    public static partial nint CGImageCreate(
        nuint width,
        nuint height,
        nuint bitsPerComponent,
        nuint bitsPerPixel,
        nuint bytesPerRow,
        nint space,
        uint bitmapInfo,
        nint provider,
        nint decode,
        [MarshalAs(UnmanagedType.Bool)] bool shouldInterpolate,
        int intent);

    [LibraryImport(LibraryName)]
    public static partial void CGContextDrawImage(nint c, CGRect rect, nint image);

    [LibraryImport(LibraryName)]
    public static partial void CGImageRelease(nint image);

    [LibraryImport(LibraryName)]
    public static partial nint CGDataProviderCreateWithData(nint info, nint data, nuint size, nint releaseData);

    [LibraryImport(LibraryName)]
    public static partial void CGDataProviderRelease(nint provider);

    [LibraryImport(LibraryName)]
    public static partial void CGContextSetTextMatrix(nint c, CGAffineTransform t);

    public const uint kCGImageAlphaPremultipliedFirst = 2;
    public const uint kCGImageAlphaNoneSkipFirst = 6;
    public const uint kCGBitmapByteOrder32Little = 2 << 12;
    public const int kCGRenderingIntentDefault = 0;
    public const int kCGInterpolationDefault = 0;
    public const int kCGInterpolationNone = 1;
    public const int kCGInterpolationLow = 2;
    public const int kCGInterpolationHigh = 3;
    public const int kCGInterpolationMedium = 4;
}

/// <summary>
/// CoreText framework P/Invoke declarations.
/// </summary>
internal static partial class CT
{
    private const string LibraryName = "/System/Library/Frameworks/CoreText.framework/CoreText";

    [LibraryImport(LibraryName)]
    public static partial nint CTFontCreateWithName(nint name, double size, nint matrix);

    [LibraryImport(LibraryName)]
    public static partial nint CTLineCreateWithAttributedString(nint attrString);

    [LibraryImport(LibraryName)]
    public static partial void CTLineDraw(nint line, nint context);

    [LibraryImport(LibraryName)]
    public static partial double CTLineGetTypographicBounds(nint line, out double ascent, out double descent, out double leading);

    [LibraryImport(LibraryName)]
    public static partial double CTFontGetAscent(nint font);

    [LibraryImport(LibraryName)]
    public static partial double CTFontGetDescent(nint font);

    private static nint _libHandle;
    private static nint _kCTFontAttributeName;

    public static nint kCTFontAttributeName
    {
        get
        {
            if (_kCTFontAttributeName == 0)
            {
                _libHandle = NativeLibrary.Load(LibraryName);
                nint ptr = NativeLibrary.GetExport(_libHandle, "kCTFontAttributeName");
                unsafe
                {
                    _kCTFontAttributeName = *(nint*)ptr;
                }
            }

            return _kCTFontAttributeName;
        }
    }
}
