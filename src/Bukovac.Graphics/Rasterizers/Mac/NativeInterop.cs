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
/// CoreFoundation framework P/Invoke declarations.
/// Shared by CoreGraphics and Metal rasterizers.
/// </summary>
internal static partial class CF
{
    private const string LibraryName = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint CFStringCreateWithCString(nint alloc, string cStr, uint encoding);

    [LibraryImport(LibraryName)]
    public static partial void CFRelease(nint cf);

    [LibraryImport(LibraryName)]
    public static partial nint CFAttributedStringCreate(nint alloc, nint str, nint attributes);

    [LibraryImport(LibraryName)]
    public static partial nint CFDictionaryCreate(
        nint allocator,
        nint[] keys,
        nint[] values,
        nint numValues,
        nint keyCallBacks,
        nint valueCallBacks);

    public const uint kCFStringEncodingUTF8 = 0x08000100;

    [LibraryImport(LibraryName)]
    public static partial int CFRunLoopRunInMode(nint mode, double seconds, [MarshalAs(UnmanagedType.Bool)] bool returnAfterSourceHandled);
}

/// <summary>
/// Objective-C runtime declarations shared by macOS rasterizers.
/// </summary>
internal static partial class ObjC
{
    private const string LibraryName = "/usr/lib/libobjc.A.dylib";

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint objc_getClass(string name);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint sel_registerName(string name);

    [LibraryImport(LibraryName, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint selector);

    [LibraryImport(LibraryName, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint selector, nint arg1);

    [LibraryImport(LibraryName, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint selector, [MarshalAs(UnmanagedType.Bool)] bool arg1);

    [LibraryImport(LibraryName, EntryPoint = "objc_msgSend")]
    public static partial void SendVoid(nint receiver, nint selector);

    [LibraryImport(LibraryName, EntryPoint = "objc_msgSend")]
    public static partial nint SendInitWindow(
        nint receiver,
        nint selector,
        CGRect contentRect,
        nuint styleMask,
        nuint backing,
        [MarshalAs(UnmanagedType.Bool)] bool defer);
}

[StructLayout(LayoutKind.Sequential)]
internal struct CGRect
{
    public double X;
    public double Y;
    public double Width;
    public double Height;

    public CGRect(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public static CGRect FromRectF(RectF r)
    {
        return new CGRect(r.X, r.Y, r.Width, r.Height);
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct CGAffineTransform
{
    public double a;
    public double b;
    public double c;
    public double d;
    public double tx;
    public double ty;

    public static CGAffineTransform Identity => new() { a = 1, d = 1 };
}
