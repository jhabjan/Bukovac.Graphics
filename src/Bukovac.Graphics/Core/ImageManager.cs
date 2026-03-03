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
/// Raw image pixel data stored in BGRA format (32 bits per pixel).
/// Rasterizers convert to their native format internally.
/// </summary>
public readonly struct ImageData(int width, int height, byte[] bgraPixels)
{
    public readonly int Width = width;
    public readonly int Height = height;
    public readonly byte[] BgraPixels = bgraPixels;
    public int Stride => Width * 4;
}

/// <summary>
/// Manages loaded image data. Each image gets a unique ImageHandle.
/// Platform rasterizers retrieve pixel data via TryGetImageData().
/// No reflection, no third-party libraries. AOT-safe.
/// </summary>
public static class ImageManager
{
    private static readonly object _lock = new();
    private static ImageData[] _images = new ImageData[16];
    private static bool[] _valid = new bool[16];
    private static int _count;

    /// <summary>
    /// Register a 32-bit BGRA pixel buffer as an image.
    /// Returns a valid ImageHandle (1-based ID). Thread-safe.
    /// </summary>
    public static ImageHandle Register(int width, int height, byte[] bgraPixels)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentException("Image dimensions must be positive.");
        }
            
        if (bgraPixels.Length < width * height * 4)
        {
            throw new ArgumentException("Pixel buffer too small for the specified dimensions.");
        }

        lock (_lock)
        {
            if (_count >= _images.Length)
            {
                int newSize = _images.Length * 2;
                Array.Resize(ref _images, newSize);
                Array.Resize(ref _valid, newSize);
            }

            int index = _count;
            _images[index] = new ImageData(width, height, bgraPixels);
            _valid[index] = true;
            _count++;
            return new ImageHandle(index + 1); // 1-based
        }
    }

    /// <summary>
    /// Retrieve image data by handle. Returns false if the handle is invalid or unregistered.
    /// </summary>
    public static bool TryGetImageData(ImageHandle handle, out ImageData data)
    {
        int index = handle.Id - 1;
        lock (_lock)
        {
            if (index >= 0 && index < _count && _valid[index])
            {
                data = _images[index];
                return true;
            }
        }
        data = default;
        return false;
    }

    /// <summary>
    /// Remove an image and release its reference. The ImageHandle becomes invalid.
    /// </summary>
    public static void Unregister(ImageHandle handle)
    {
        int index = handle.Id - 1;
        lock (_lock)
        {
            if (index >= 0 && index < _count)
            {
                _images[index] = default;
                _valid[index] = false;
            }
        }
    }
}
