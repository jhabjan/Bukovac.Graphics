using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_27(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("27-maze-backtracker", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x0D, 0x11, 0x0F));
                    c.FillRoundedRectangle(new SolidBrush(C(0x15, 0x1E, 0x1A, 255)), 22, 22, w - 44, h - 44, 14);
                    c.DrawString("Depth-First Maze", title, new SolidBrush(ColorF.FromRgb(0xE5, 0xFF, 0xEE)), 32, 30);
                    int cols = 34;
                    int rows = 18;
                    float cell = MathF.Min((w - 90f) / cols, (h - 130f) / rows);
                    float ox = 44;
                    float oy = 80;
                    var maze = GenerateMaze(cols, rows, 1337);
                    var wall = new Pen(ColorF.FromRgb(0x99, 0xF0, 0xBA), 1);
                    for (int y = 0; y < rows; y++)
                    {
                        for (int x = 0; x < cols; x++)
                        {
                            int idx = (y * cols) + x;
                            int bits = maze[idx];
                            float x0 = ox + (x * cell);
                            float y0 = oy + (y * cell);
                            float x1 = x0 + cell;
                            float y1 = y0 + cell;
                            if ((bits & 1) != 0) c.DrawLine(wall, x0, y0, x1, y0);
                            if ((bits & 2) != 0) c.DrawLine(wall, x1, y0, x1, y1);
                            if ((bits & 4) != 0) c.DrawLine(wall, x1, y1, x0, y1);
                            if ((bits & 8) != 0) c.DrawLine(wall, x0, y1, x0, y0);
                        }
                    }
                }))
        );
    }
}

