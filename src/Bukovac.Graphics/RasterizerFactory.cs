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
/// Creates rasterizer instances. Switch-based factory — no reflection, AOT-safe.
/// </summary>
public static class RasterizerFactory
{

#pragma warning disable CA1416 // Platform compatibility — this factory dispatches by OS at runtime

    public static IRasterizer Create(RasterizerKind kind)
    {
        var resolved = GraphicsConfig.ResolveRasterizerKind(kind);

        return resolved switch
        {
            RasterizerKind.WindowsGDI => new Rasterizers.Windows.GdiRasterizer(),
            RasterizerKind.WindowsDirect2D => new Rasterizers.Windows.Direct2DRasterizer(),
            RasterizerKind.WindowsOpenGL => new Rasterizers.Windows.OpenGLRasterizer(),
            RasterizerKind.LinuxCairo => new Rasterizers.Linux.CairoRasterizer(),
            RasterizerKind.LinuxOpenGL => new Rasterizers.Linux.OpenGLRasterizer(),
            RasterizerKind.MacCoreGraphics => new Rasterizers.Mac.CoreGraphicsRasterizer(),
            RasterizerKind.MacMetal => new Rasterizers.Mac.MetalRasterizer(),
            _ => throw new PlatformNotSupportedException($"Rasterizer '{resolved}' is not available on this platform."),
        };

#pragma warning restore CA1416

    }
}
