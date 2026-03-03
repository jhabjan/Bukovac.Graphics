using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_17(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("17-retro-terminal", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x0A, 0x12, 0x0A));
                    c.FillRoundedRectangle(new SolidBrush(C(0x10, 0x1B, 0x10, 255)), 24, 24, w - 48, h - 48, 14);
                    c.DrawRoundedRectangle(new Pen(ColorF.FromRgb(0x87, 0xFF, 0x9A), 2), 24, 24, w - 48, h - 48, 14);
                    for (int y = 56; y < h - 40; y += 18)
                        c.DrawLine(new Pen(ColorF.FromRgb(0x1E, 0x3E, 0x1E), 1), 34, y, w - 34, y);
                    c.DrawString("$ render --all-rasterizers --format=png", new FontSpec(monoFamily, 18), new SolidBrush(ColorF.FromRgb(0xA8, 0xFF, 0xA8)), 44, 72);
                    c.DrawString("> GDI       OK", new FontSpec(monoFamily, 17), new SolidBrush(ColorF.FromRgb(0xD7, 0xFF, 0xD7)), 44, 124);
                    c.DrawString("> Direct2D  OK", new FontSpec(monoFamily, 17), new SolidBrush(ColorF.FromRgb(0xD7, 0xFF, 0xD7)), 44, 154);
                    c.DrawString("> Cairo     OK", new FontSpec(monoFamily, 17), new SolidBrush(ColorF.FromRgb(0xD7, 0xFF, 0xD7)), 44, 184);
                    c.DrawString("> OpenGL    OK", new FontSpec(monoFamily, 17), new SolidBrush(ColorF.FromRgb(0xD7, 0xFF, 0xD7)), 44, 214);
                    c.DrawString("> CoreGraphics / Metal", new FontSpec(monoFamily, 17), new SolidBrush(ColorF.FromRgb(0xD7, 0xFF, 0xD7)), 44, 244);
                }))
        );
    }
}

