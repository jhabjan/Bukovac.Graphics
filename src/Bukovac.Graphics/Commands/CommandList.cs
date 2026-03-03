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

namespace Bukovac.Graphics.Commands;

/// <summary>
/// Collects draw commands during a frame. Cleared at BeginFrame().
/// </summary>
public sealed class CommandList
{
    private DrawCommand[] _commands;
    private int _count;

    public CommandList(int initialCapacity = 256)
    {
        _commands = new DrawCommand[initialCapacity];
        _count = 0;
    }

    public int Count => _count;

    public void Add(DrawCommand cmd)
    {
        if (_count == _commands.Length)
        {
            var newArr = new DrawCommand[_commands.Length * 2];
            Array.Copy(_commands, newArr, _count);
            _commands = newArr;
        }
        _commands[_count++] = cmd;
    }

    public ReadOnlySpan<DrawCommand> AsSpan() => _commands.AsSpan(0, _count);

    public ref DrawCommand this[int index] => ref _commands[index];

    public void Clear()
    {
        // Clear references (strings etc.) to avoid holding onto memory
        Array.Clear(_commands, 0, _count);
        _count = 0;
    }
}
