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
/// Represents a color with floating-point RGBA components (0.0–1.0).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct ColorF(float r, float g, float b, float a = 1f)
{
    public readonly float R = r;
    public readonly float G = g;
    public readonly float B = b;
    public readonly float A = a;

    // Common colors
    public static readonly ColorF Transparent = new(0f, 0f, 0f, 0f);
    public static readonly ColorF Black = new(0f, 0f, 0f);
    public static readonly ColorF White = new(1f, 1f, 1f);
    public static readonly ColorF Red = new(1f, 0f, 0f);
    public static readonly ColorF Green = new(0f, 1f, 0f);
    public static readonly ColorF Blue = new(0f, 0f, 1f);
    public static readonly ColorF Yellow = new(1f, 1f, 0f);
    public static readonly ColorF Cyan = new(0f, 1f, 1f);
    public static readonly ColorF Magenta = new(1f, 0f, 1f);
    public static readonly ColorF Gray = new(0.5f, 0.5f, 0.5f);
    public static readonly ColorF DarkGray = new(0.25f, 0.25f, 0.25f);
    public static readonly ColorF LightGray = new(0.75f, 0.75f, 0.75f);
    public static readonly ColorF CornflowerBlue = new(0.392f, 0.584f, 0.929f);
    public static readonly ColorF Orange = new(1f, 0.647f, 0f);

    public static ColorF FromArgb(byte a, byte r, byte g, byte b) => new(r / 255f, g / 255f, b / 255f, a / 255f);

    public static ColorF FromRgb(byte r, byte g, byte b) => new(r / 255f, g / 255f, b / 255f, 1f);

    public byte ToByte(float component) => (byte)(Math.Clamp(component, 0f, 1f) * 255f + 0.5f);

    public uint ToArgb32() => ((uint)ToByte(A) << 24) | ((uint)ToByte(R) << 16) | ((uint)ToByte(G) << 8) | ToByte(B);

    public uint ToBgra32() => ((uint)ToByte(B) << 24) | ((uint)ToByte(G) << 16) | ((uint)ToByte(R) << 8) | ToByte(A);

    public uint ToAbgr32() => ((uint)ToByte(A) << 24) | ((uint)ToByte(B) << 16) | ((uint)ToByte(G) << 8) | ToByte(R);

    public ColorF WithAlpha(float alpha) => new(R, G, B, alpha);

    /// <summary>
    /// Parses an HTML hex color string: "#RGB", "#RRGGBB", or "#AARRGGBB".
    /// </summary>
    public static ColorF FromHex(string hex)
    {
        ArgumentNullException.ThrowIfNull(hex);

        ReadOnlySpan<char> span = hex.AsSpan();
        if (span.Length > 0 && span[0] == '#')
            span = span[1..];

        switch (span.Length)
        {
            case 3: // #RGB
            {
                byte rv = ParseHexNibble(span[0]);
                byte gv = ParseHexNibble(span[1]);
                byte bv = ParseHexNibble(span[2]);
                return FromRgb((byte)(rv | (rv << 4)), (byte)(gv | (gv << 4)), (byte)(bv | (bv << 4)));
            }
            case 6: // #RRGGBB
            {
                byte rv = ParseHexByte(span[0], span[1]);
                byte gv = ParseHexByte(span[2], span[3]);
                byte bv = ParseHexByte(span[4], span[5]);
                return FromRgb(rv, gv, bv);
            }
            case 8: // #AARRGGBB
            {
                byte av = ParseHexByte(span[0], span[1]);
                byte rv = ParseHexByte(span[2], span[3]);
                byte gv = ParseHexByte(span[4], span[5]);
                byte bv = ParseHexByte(span[6], span[7]);
                return FromArgb(av, rv, gv, bv);
            }
            default:
                throw new FormatException($"Invalid hex color format: '{hex}'. Expected #RGB, #RRGGBB, or #AARRGGBB.");
        }
    }

    private static byte ParseHexNibble(char c) => c switch
    {
        >= '0' and <= '9' => (byte)(c - '0'),
        >= 'a' and <= 'f' => (byte)(c - 'a' + 10),
        >= 'A' and <= 'F' => (byte)(c - 'A' + 10),
        _ => throw new FormatException($"Invalid hex character: '{c}'"),
    };

    private static byte ParseHexByte(char hi, char lo) => (byte)((ParseHexNibble(hi) << 4) | ParseHexNibble(lo));

    public override string ToString() => $"ColorF(R={R:F3}, G={G:F3}, B={B:F3}, A={A:F3})";
}
