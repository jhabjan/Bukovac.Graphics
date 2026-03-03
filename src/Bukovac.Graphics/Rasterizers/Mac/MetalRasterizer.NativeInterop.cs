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
/// Metal framework P/Invoke declarations.
/// </summary>
internal static partial class Metal
{
    private const string LibraryName = "/System/Library/Frameworks/Metal.framework/Metal";

    [LibraryImport(LibraryName)]
    public static partial nint MTLCreateSystemDefaultDevice();
}

/// <summary>
/// Objective-C runtime declarations used by Metal rasterizer.
/// </summary>
internal static partial class ObjC
{
    [LibraryImport(LibraryName, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint selector, nint arg1, nint arg2);

    [LibraryImport(LibraryName, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint selector, nint arg1, nint arg2, nint arg3);

    [LibraryImport(LibraryName, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint selector, nint arg1, nint arg2, nint arg3, nint arg4);

    [LibraryImport(LibraryName, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint selector, nuint arg1);

    [LibraryImport(LibraryName, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint selector, nuint arg1, nuint arg2);

    [LibraryImport(LibraryName, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint selector, nuint arg1, nuint arg2, nuint arg3);

    [LibraryImport(LibraryName, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint selector, nuint arg1, nuint arg2, [MarshalAs(UnmanagedType.Bool)] bool arg3);

    [LibraryImport(LibraryName, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint selector, nuint arg1, nuint arg2, nuint arg3, [MarshalAs(UnmanagedType.Bool)] bool arg4);

    [LibraryImport(LibraryName, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint selector, CGSize arg1);

    [LibraryImport(LibraryName, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint selector, MTLRegion arg1, nuint arg2, nint arg3, nuint arg4);

    [LibraryImport(LibraryName, EntryPoint = "objc_msgSend")]
    public static partial nint Send(
        nint receiver,
        nint selector,
        nint arg1,
        nuint arg2,
        nuint arg3,
        MTLOrigin arg4,
        MTLSize arg5,
        nint arg6,
        nuint arg7,
        nuint arg8,
        MTLOrigin arg9);

    [LibraryImport(LibraryName, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint selector, nint arg1, nuint arg2, nint arg3, nuint arg4);

    [LibraryImport(LibraryName, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint selector, nint arg1, nuint arg2, nuint arg3);

    [LibraryImport(LibraryName, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint selector, nint arg1, nuint arg2, nuint arg3, nuint arg4);

    [LibraryImport(LibraryName, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint selector, nint arg1, nuint arg2, nuint arg3, nuint arg4, nuint arg5);

    [LibraryImport(LibraryName, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint selector, nint arg1, nuint arg2, nint arg3, nuint arg4, nuint arg5);

    [LibraryImport(LibraryName, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint selector, MTLScissorRect arg1);

    [LibraryImport(LibraryName, EntryPoint = "objc_msgSend")]
    public static partial nint Send(nint receiver, nint selector, MTLClearColor arg1);

    [LibraryImport(LibraryName, EntryPoint = "objc_msgSend")]
    public static partial void SendVoid(nint receiver, nint selector, nint arg1, nuint arg2, MTLRegion arg3, nuint arg4);
}

[StructLayout(LayoutKind.Sequential)]
internal struct CGSize
{
    public double Width;
    public double Height;

    public CGSize(double width, double height)
    {
        Width = width;
        Height = height;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct MTLOrigin
{
    public nuint x;
    public nuint y;
    public nuint z;

    public MTLOrigin(nuint x, nuint y, nuint z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct MTLSize
{
    public nuint width;
    public nuint height;
    public nuint depth;

    public MTLSize(nuint width, nuint height, nuint depth)
    {
        this.width = width;
        this.height = height;
        this.depth = depth;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct MTLRegion
{
    public MTLOrigin origin;
    public MTLSize size;

    public MTLRegion(MTLOrigin origin, MTLSize size)
    {
        this.origin = origin;
        this.size = size;
    }

    public static MTLRegion Make2D(int x, int y, int width, int height)
    {
        return new MTLRegion(new MTLOrigin((nuint)x, (nuint)y, 0), new MTLSize((nuint)width, (nuint)height, 1));
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct MTLScissorRect
{
    public nuint x;
    public nuint y;
    public nuint width;
    public nuint height;

    public MTLScissorRect(nuint x, nuint y, nuint width, nuint height)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct MTLClearColor
{
    public double red;
    public double green;
    public double blue;
    public double alpha;

    public MTLClearColor(double red, double green, double blue, double alpha)
    {
        this.red = red;
        this.green = green;
        this.blue = blue;
        this.alpha = alpha;
    }
}
