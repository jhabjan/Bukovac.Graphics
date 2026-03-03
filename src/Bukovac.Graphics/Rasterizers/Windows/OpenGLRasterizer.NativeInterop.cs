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

namespace Bukovac.Graphics.Rasterizers.Windows.OpenGL
{
internal static partial class WinUser32
{
    private const string LibraryName = "user32.dll";

    [LibraryImport(LibraryName)]
    public static partial nint GetDC(nint hWnd);

    [LibraryImport(LibraryName)]
    public static partial int ReleaseDC(nint hWnd, nint hDC);

    [LibraryImport(LibraryName, EntryPoint = "CreateWindowExW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint CreateWindowEx(
        uint exStyle,
        string className,
        string windowName,
        uint style,
        int x,
        int y,
        int width,
        int height,
        nint parent,
        nint menu,
        nint instance,
        nint param);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyWindow(nint hWnd);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool MoveWindow(nint hWnd, int x, int y, int width, int height, [MarshalAs(UnmanagedType.Bool)] bool repaint);

    public const uint WS_POPUP = 0x80000000;
}

internal static partial class WinGdi32
{
    private const string LibraryName = "gdi32.dll";

    [LibraryImport(LibraryName)]
    public static partial int ChoosePixelFormat(nint hdc, ref PIXELFORMATDESCRIPTOR ppfd);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetPixelFormat(nint hdc, int format, ref PIXELFORMATDESCRIPTOR ppfd);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SwapBuffers(nint hdc);

    public const uint PFD_DRAW_TO_WINDOW = 0x00000004;
    public const uint PFD_SUPPORT_OPENGL = 0x00000020;
    public const uint PFD_DOUBLEBUFFER = 0x00000001;
    public const byte PFD_TYPE_RGBA = 0;
    public const sbyte PFD_MAIN_PLANE = 0;
}

internal static partial class Wgl32
{
    private const string LibraryName = "opengl32.dll";

    [LibraryImport(LibraryName)]
    public static partial nint wglCreateContext(nint hdc);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool wglDeleteContext(nint hglrc);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool wglMakeCurrent(nint hdc, nint hglrc);

    [LibraryImport(LibraryName)]
    public static partial void glViewport(int x, int y, int width, int height);

    [LibraryImport(LibraryName)]
    public static partial void glClearColor(float red, float green, float blue, float alpha);

    [LibraryImport(LibraryName)]
    public static partial void glClear(uint mask);

    [LibraryImport(LibraryName)]
    public static partial void glMatrixMode(uint mode);

    [LibraryImport(LibraryName)]
    public static partial void glLoadIdentity();

    [LibraryImport(LibraryName)]
    public static partial void glOrtho(double left, double right, double bottom, double top, double zNear, double zFar);

    [LibraryImport(LibraryName)]
    public static partial void glEnable(uint cap);

    [LibraryImport(LibraryName)]
    public static partial void glDisable(uint cap);

    [LibraryImport(LibraryName)]
    public static partial void glGenTextures(int count, out uint textures);

    [LibraryImport(LibraryName)]
    public static partial void glDeleteTextures(int count, ref uint textures);

    [LibraryImport(LibraryName)]
    public static partial void glBindTexture(uint target, uint texture);

    [LibraryImport(LibraryName)]
    public static partial void glTexParameteri(uint target, uint pname, int param);

    [LibraryImport(LibraryName)]
    public static partial void glBlendFunc(uint sfactor, uint dfactor);

    [LibraryImport(LibraryName)]
    public static partial void glLineWidth(float width);

    [LibraryImport(LibraryName)]
    public static partial void glScissor(int x, int y, int width, int height);

    [LibraryImport(LibraryName)]
    public static partial void glColor4f(float red, float green, float blue, float alpha);

    [LibraryImport(LibraryName)]
    public static partial void glBegin(uint mode);

    [LibraryImport(LibraryName)]
    public static partial void glEnd();

    [LibraryImport(LibraryName)]
    public static partial void glTexCoord2f(float s, float t);

    [LibraryImport(LibraryName)]
    public static partial void glVertex2f(float x, float y);

    [LibraryImport(LibraryName)]
    public static partial void glFlush();

    [LibraryImport(LibraryName)]
    public static partial void glReadPixels(int x, int y, int width, int height, uint format, uint type, nint pixels);

    [LibraryImport(LibraryName)]
    public static partial void glTexImage2D(
        uint target, int level, int internalFormat, int width, int height, int border, uint format, uint type, nint pixels);

    public const uint GL_COLOR_BUFFER_BIT = 0x00004000;
    public const uint GL_PROJECTION = 0x1701;
    public const uint GL_MODELVIEW = 0x1700;
    public const uint GL_TEXTURE_2D = 0x0DE1;
    public const uint GL_BLEND = 0x0BE2;
    public const uint GL_SCISSOR_TEST = 0x0C11;
    public const uint GL_SRC_ALPHA = 0x0302;
    public const uint GL_ONE_MINUS_SRC_ALPHA = 0x0303;
    public const uint GL_TEXTURE_MIN_FILTER = 0x2801;
    public const uint GL_TEXTURE_MAG_FILTER = 0x2800;
    public const uint GL_TEXTURE_WRAP_S = 0x2802;
    public const uint GL_TEXTURE_WRAP_T = 0x2803;
    public const uint GL_CLAMP_TO_EDGE = 0x812F;
    public const uint GL_NEAREST = 0x2600;
    public const uint GL_LINEAR = 0x2601;
    public const uint GL_LINE_SMOOTH = 0x0B20;
    public const uint GL_DITHER = 0x0BD0;
    public const uint GL_QUADS = 0x0007;
    public const uint GL_LINES = 0x0001;
    public const uint GL_LINE_LOOP = 0x0002;
    public const uint GL_TRIANGLE_FAN = 0x0006;
    public const uint GL_BGRA = 0x80E1;
    public const uint GL_UNSIGNED_BYTE = 0x1401;
    public const int GL_RGBA8 = 0x8058;
}

[StructLayout(LayoutKind.Sequential)]
internal struct PIXELFORMATDESCRIPTOR
{
    public ushort nSize;
    public ushort nVersion;
    public uint dwFlags;
    public byte iPixelType;
    public byte cColorBits;
    public byte cRedBits;
    public byte cRedShift;
    public byte cGreenBits;
    public byte cGreenShift;
    public byte cBlueBits;
    public byte cBlueShift;
    public byte cAlphaBits;
    public byte cAlphaShift;
    public byte cAccumBits;
    public byte cAccumRedBits;
    public byte cAccumGreenBits;
    public byte cAccumBlueBits;
    public byte cAccumAlphaBits;
    public byte cDepthBits;
    public byte cStencilBits;
    public byte cAuxBuffers;
    public sbyte iLayerType;
    public byte bReserved;
    public uint dwLayerMask;
    public uint dwVisibleMask;
    public uint dwDamageMask;
}
}
