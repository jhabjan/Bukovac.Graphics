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
/// X11 library P/Invoke declarations.
/// </summary>
internal static partial class X11
{
    private const string LibraryName = "libX11.so.6";

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint XOpenDisplay(string? displayName);

    [LibraryImport(LibraryName)]
    public static partial int XCloseDisplay(nint display);

    [LibraryImport(LibraryName)]
    public static partial int XDefaultScreen(nint display);

    [LibraryImport(LibraryName)]
    public static partial nint XDefaultVisual(nint display, int screenNumber);

    [LibraryImport(LibraryName)]
    public static partial nint XRootWindow(nint display, int screenNumber);

    [LibraryImport(LibraryName)]
    public static partial int XDefaultDepth(nint display, int screenNumber);

    [LibraryImport(LibraryName)]
    public static partial nint XBlackPixel(nint display, int screenNumber);

    [LibraryImport(LibraryName)]
    public static partial nint XWhitePixel(nint display, int screenNumber);

    [LibraryImport(LibraryName)]
    public static partial nint XCreateSimpleWindow(
        nint display, nint parent,
        int x, int y, uint width, uint height,
        uint borderWidth, nint border, nint background);

    [LibraryImport(LibraryName)]
    public static partial int XMapWindow(nint display, nint window);

    [LibraryImport(LibraryName)]
    public static partial int XDestroyWindow(nint display, nint window);

    [LibraryImport(LibraryName)]
    public static partial int XResizeWindow(nint display, nint window, uint width, uint height);

    [LibraryImport(LibraryName)]
    public static partial int XSelectInput(nint display, nint window, nint eventMask);

    [LibraryImport(LibraryName)]
    public static partial int XNextEvent(nint display, nint eventReturn);

    [LibraryImport(LibraryName)]
    public static partial int XPending(nint display);

    [LibraryImport(LibraryName)]
    public static partial int XFlush(nint display);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint XInternAtom(nint display, string atomName, [MarshalAs(UnmanagedType.Bool)] bool onlyIfExists);

    [LibraryImport(LibraryName)]
    public static partial int XSetWMProtocols(nint display, nint window, nint[] protocols, int count);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int XStoreName(nint display, nint window, string windowName);

    [LibraryImport(LibraryName)]
    public static partial int XFree(nint data);

    // Event masks
    public const nint ExposureMask = 1 << 15;
    public const nint StructureNotifyMask = 1 << 17;
    public const nint KeyPressMask = 1 << 0;

    // Event types
    public const int Expose = 12;
    public const int ConfigureNotify = 22;
    public const int ClientMessage = 33;
    public const int DestroyNotify = 17;
}


/// <summary>
/// X11 XConfigureEvent — used to detect window resize.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct XConfigureEvent
{
    public int type;
    public nuint serial;
    public int send_event;
    public nint display;
    public nint eventWindow;
    public nint window;
    public int x;
    public int y;
    public int width;
    public int height;
    public int border_width;
    public nint above;
    public int override_redirect;
}

/// <summary>
/// X11 XClientMessageEvent — used for WM_DELETE_WINDOW.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct XClientMessageEvent
{
    public int type;
    public nuint serial;
    public int send_event;
    public nint display;
    public nint window;
    public nint message_type;
    public int format;
    public nint data0;
    public nint data1;
    public nint data2;
    public nint data3;
    public nint data4;
}

/// <summary>
/// X11 XEvent union — use LayoutKind.Explicit to overlay event types.
/// Sized to 192 bytes to cover all X11 event union variants.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 192)]
internal struct XEvent
{
    [FieldOffset(0)] public int type;
    [FieldOffset(0)] public XConfigureEvent xconfigure;
    [FieldOffset(0)] public XClientMessageEvent xclient;
}
