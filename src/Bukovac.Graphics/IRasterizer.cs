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
using Bukovac.Graphics.Commands;

namespace Bukovac.Graphics;

/// <summary>
/// Interface for platform-specific rendering backends.
/// Rasterizers execute recorded draw commands.
/// </summary>
public interface IRasterizer : IDisposable
{
    /// <summary>
    /// Initialize the rasterizer for a native window target.
    /// </summary>
    void Initialize(NativeWindowHandle window, int width, int height);

    /// <summary>
    /// Initialize the rasterizer for an off-screen bitmap target.
    /// </summary>
    void InitializeBitmap(int width, int height);

    /// <summary>
    /// Handle window resize.
    /// </summary>
    void Resize(int width, int height);

    /// <summary>
    /// Begin a new frame / drawing session.
    /// </summary>
    void BeginFrame();

    /// <summary>
    /// Execute all recorded commands and present.
    /// </summary>
    void EndFrame(ReadOnlySpan<DrawCommand> commands);

    /// <summary>
    /// Measure text. Called synchronously (not deferred).
    /// </summary>
    Vector2 MeasureString(string text, FontSpec font, float maxWidth);

    /// <summary>
    /// Measure text with format flags. Default delegates to the flagless overload.
    /// </summary>
    Vector2 MeasureString(string text, FontSpec font, float maxWidth, TextFormatFlags flags)
        => MeasureString(text, font, maxWidth);

    /// <summary>
    /// Returns the line height (cell height) for a font.
    /// Default implementation approximates via MeasureString.
    /// </summary>
    float GetFontHeight(FontSpec font) => MeasureString("Mq", font, float.PositiveInfinity).Y;

    /// <summary>
    /// Returns the device DPI for the current render target.
    /// Default returns 96 (1.0x scale). Platform rasterizers override for actual DPI.
    /// </summary>
    float GetDpi() => 96f;

    /// <summary>
    /// Sets host DPI when the rendering host wants to control DPI explicitly.
    /// Default is a no-op.
    /// </summary>
    void SetDpi(float dpi) { }

    /// <summary>
    /// Apply quality/rendering mode settings. Called once per frame after BeginFrame.
    /// Default implementation is a no-op; backends override for platform-specific behavior.
    /// </summary>
    void ApplyQualitySettings(InterpolationMode interpolation, SmoothingMode smoothing,
        PixelOffsetMode pixelOffset, CompositingQuality compositing) { }

    /// <summary>
    /// Initialize the rasterizer to render into an externally provided HDC (e.g. from WinForms PaintEventArgs).
    /// Default implementation forwards to the sub-rectangle overload at origin (0,0).
    /// </summary>
    void InitializeFromHdc(nint hdc, int width, int height)
        => InitializeFromHdc(hdc, 0, 0, width, height);

    /// <summary>
    /// Initialize the rasterizer to render into a sub-rectangle of an externally provided HDC.
    /// Default implementation throws NotSupportedException.
    /// </summary>
    void InitializeFromHdc(nint hdc, int x, int y, int width, int height)
    {
        throw new NotSupportedException($"{GetType().Name} does not support InitializeFromHdc.");
    }

    /// <summary>
    /// Attempts to read the current render target pixels as 32-bit BGRA (top-down).
    /// </summary>
    bool TryCopyPixelsBgra(out int width, out int height, out byte[] bgraPixels);
}
