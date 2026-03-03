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
/// Represents a rectangle with floating-point coordinates.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct RectF(float x, float y, float width, float height)
{
    public readonly float X = x;
    public readonly float Y = y;
    public readonly float Width = width;
    public readonly float Height = height;

    public float Left => X;
    public float Top => Y;
    public float Right => X + Width;
    public float Bottom => Y + Height;

    public PointF Location => new(X, Y);
    public PointF Center => new(X + Width * 0.5f, Y + Height * 0.5f);

    public static readonly RectF Empty = new(0f, 0f, 0f, 0f);

    public static RectF FromLTRB(float left, float top, float right, float bottom) => new(left, top, right - left, bottom - top);

    public bool Contains(PointF point) => point.X >= X && point.X < Right && point.Y >= Y && point.Y < Bottom;

    public bool Contains(float px, float py) => px >= X && px < Right && py >= Y && py < Bottom;

    public bool IntersectsWith(RectF other) => X < other.Right && Right > other.X && Y < other.Bottom && Bottom > other.Y;

    public RectF Inflate(float dx, float dy) => new(X - dx, Y - dy, Width + 2 * dx, Height + 2 * dy);

    public RectF Offset(float dx, float dy) => new(X + dx, Y + dy, Width, Height);

    public override string ToString() => $"RectF({X:F2}, {Y:F2}, {Width:F2}, {Height:F2})";
}
