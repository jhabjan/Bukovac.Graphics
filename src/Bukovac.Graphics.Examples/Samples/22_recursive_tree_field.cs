using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_22(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("22-recursive-tree-field", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x0E, 0x10, 0x17));
                    c.FillRectangle(new SolidBrush(C(0x12, 0x1A, 0x2A, 255)), 0, 0, w, h * 0.62f);
                    c.FillRectangle(new SolidBrush(C(0x1A, 0x21, 0x12, 255)), 0, h * 0.62f, w, h * 0.38f);
                    c.DrawString("Recursive Tree Field", title, new SolidBrush(ColorF.FromRgb(0xED, 0xF6, 0xFF)), 34, 28);
                    for (int i = 0; i < 7; i++)
                    {
                        float x = 110 + (i * ((w - 220) / 6f));
                        float len = 92 + (i % 3) * 14;
                        DrawBranchTree(c, x, h - 56, -MathF.PI / 2f, len, 8, i * 0.14f);
                    }
                }))
        );
    }
}

