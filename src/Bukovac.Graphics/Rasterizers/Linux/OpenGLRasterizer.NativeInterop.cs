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
/// GLX/OpenGL library P/Invoke declarations for Linux.
/// </summary>
internal static partial class Glx
{
    private const string LibraryName = "libGL.so.1";

    [LibraryImport(LibraryName)]
    public static partial nint glXChooseVisual(nint display, int screen, int[] attribList);

    [LibraryImport(LibraryName)]
    public static partial nint glXCreateContext(nint display, nint vis, nint shareList, [MarshalAs(UnmanagedType.Bool)] bool direct);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool glXMakeCurrent(nint display, nint drawable, nint ctx);

    [LibraryImport(LibraryName)]
    public static partial void glXSwapBuffers(nint display, nint drawable);

    [LibraryImport(LibraryName)]
    public static partial void glXDestroyContext(nint display, nint ctx);

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

    public const int None = 0;
    public const int GLX_RGBA = 4;
    public const int GLX_DOUBLEBUFFER = 5;
    public const int GLX_DEPTH_SIZE = 12;
    public const int GLX_STENCIL_SIZE = 13;

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
