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

namespace Bukovac.Graphics;

/// <summary>
/// Identifies the rasterizer backend to use.
/// </summary>
public enum RasterizerKind
{
    /// <summary>Auto-select based on platform.</summary>
    Default = 0,

    // Windows
    WindowsGDI = 100,
    WindowsDirect2D = 101,
    WindowsOpenGL = 102,

    // Linux
    LinuxCairo = 200,
    LinuxOpenGL = 201,

    // macOS
    MacCoreGraphics = 300,
    MacMetal = 301,
}

/// <summary>
/// Global configuration for Bukovac.Graphics. All settings are static for performance.
/// </summary>
public static class GraphicsConfig
{
    private static readonly RasterizerKind[] WindowsRasterizers =
    [
        RasterizerKind.WindowsGDI,
        RasterizerKind.WindowsDirect2D,
        RasterizerKind.WindowsOpenGL,
    ];

    private static readonly RasterizerKind[] LinuxRasterizers =
    [
        RasterizerKind.LinuxCairo,
        RasterizerKind.LinuxOpenGL,
    ];

    private static readonly RasterizerKind[] MacRasterizers =
    [
        RasterizerKind.MacCoreGraphics,
        RasterizerKind.MacMetal,
    ];

    private static readonly RasterizerKind[] EmptyRasterizers = [];

    /// <summary>
    /// The default rasterizer kind used when Canvas is created without specifying one.
    /// </summary>
    public static RasterizerKind RasterizerKind { get; set; } = RasterizerKind.Default;

    /// <summary>
    /// Default DPI scale factor.
    /// </summary>
    public static float DefaultDpiScale { get; set; } = 1.0f;

    /// <summary>
    /// Whether to enable anti-aliasing by default.
    /// </summary>
    public static bool DefaultAntiAlias { get; set; } = true;

    /// <summary>
    /// Default text rendering quality.
    /// </summary>
    public static TextRenderingHint DefaultTextRenderingHint { get; set; } = TextRenderingHint.ClearType;

    /// <summary>
    /// Resolves the actual rasterizer kind from Default based on the current OS.
    /// </summary>
    public static RasterizerKind ResolveRasterizerKind(RasterizerKind kind)
    {
        if (kind != RasterizerKind.Default)
            return kind;

        if (OperatingSystem.IsWindows())
            return RasterizerKind.WindowsGDI;
        if (OperatingSystem.IsLinux())
            return RasterizerKind.LinuxCairo;
        if (OperatingSystem.IsMacOS())
            return RasterizerKind.MacCoreGraphics;

        return RasterizerKind.WindowsGDI; // fallback
    }

    /// <summary>
    /// Returns rasterizers supported on the currently running OS.
    /// </summary>
    public static IReadOnlyList<RasterizerKind> GetAvailableRasterizers()
    {
        if (OperatingSystem.IsWindows())
            return WindowsRasterizers;
        if (OperatingSystem.IsLinux())
            return LinuxRasterizers;
        if (OperatingSystem.IsMacOS())
            return MacRasterizers;

        return EmptyRasterizers;
    }

    /// <summary>
    /// Returns true when a rasterizer is supported on the currently running OS.
    /// </summary>
    public static bool IsRasterizerAvailable(RasterizerKind kind)
    {
        if (kind == RasterizerKind.Default)
            return true;

        var available = GetAvailableRasterizers();
        for (int i = 0; i < available.Count; i++)
        {
            if (available[i] == kind)
                return true;
        }

        return false;
    }
}

public enum TextRenderingHint
{
    SystemDefault = 0,
    AntiAlias = 1,
    ClearType = 2,
}
