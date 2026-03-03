using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_07(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("07-glyph-run", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x1E, 0x1A, 0x10));
                    c.FillRectangle(new SolidBrush(C(0x3A, 0x2A, 0x14, 255)), 20, 120, w - 40, 160);
                    c.DrawRectangle(new Pen(ColorF.FromRgb(0xFF, 0xC8, 0x6A), 2), 20, 120, w - 40, 160);
                    for (int x = 40; x < w - 40; x += 18)
                    {
                        c.DrawLine(new Pen(ColorF.FromRgb(0x5C, 0x4A, 0x2C), 1), x, 120, x, 280);
                    }
                    string text = "MONOSPACE GRID 0123456789";
                    int[] advances = new int[text.Length];
                    Array.Fill(advances, 18);
                    c.DrawGlyphRun(text, advances, new FontSpec(monoFamily, 18), new SolidBrush(ColorF.FromRgb(0xFF, 0xE7, 0x9A)), 40, 160);
                    c.DrawGlyphRunUniform("Uniform advance demo", 16, new FontSpec(monoFamily, 16), new SolidBrush(ColorF.FromRgb(0xB1, 0xF0, 0xC1)), 40, 220);
                }))
        );
    }
}

