using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_31(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("31-recursive-tiles", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x12, 0x0F, 0x09));
                    c.FillRoundedRectangle(new SolidBrush(C(0x21, 0x1A, 0x10, 255)), 20, 20, w - 40, h - 40, 14);
                    c.DrawString("Recursive Tile Subdivision", title, new SolidBrush(ColorF.FromRgb(0xFF, 0xF3, 0xD8)), 34, 28);
                    DrawRecursiveTiles(c, new RectF(46, 84, w - 92, h - 132), 5, 7781);
                }))
        );
    }
}

