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

using System.Numerics;
using System.Runtime.InteropServices;

namespace Bukovac.Graphics;

/// <summary>
/// Represents a point with floating-point X and Y coordinates.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct PointF(float x, float y)
{
    public readonly float X = x;
    public readonly float Y = y;

    public static readonly PointF Zero = new(0f, 0f);

    public static PointF operator +(PointF a, PointF b) => new(a.X + b.X, a.Y + b.Y);
    public static PointF operator -(PointF a, PointF b) => new(a.X - b.X, a.Y - b.Y);
    public static PointF operator *(PointF p, float s) => new(p.X * s, p.Y * s);

    public Vector2 ToVector2() => new(X, Y);
    public static PointF FromVector2(Vector2 v) => new(v.X, v.Y);

    public override string ToString() => $"PointF({X:F2}, {Y:F2})";
}
