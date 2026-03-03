using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_18(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("18-renderer-showcase", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x15, 0x14, 0x12));
                    float col = (w - 80) / 3f;
                    c.FillRoundedRectangle(new SolidBrush(C(0x2A, 0x23, 0x1F, 255)), 20, 40, col, h - 80, 14);
                    c.FillRoundedRectangle(new SolidBrush(C(0x1F, 0x2A, 0x2A, 255)), 30 + col, 40, col, h - 80, 14);
                    c.FillRoundedRectangle(new SolidBrush(C(0x24, 0x1F, 0x2D, 255)), 40 + col * 2, 40, col, h - 80, 14);
                    c.DrawString("CPU", title, new SolidBrush(ColorF.FromRgb(0xFF, 0xD0, 0x9B)), 42, 58);
                    c.DrawString("GPU", title, new SolidBrush(ColorF.FromRgb(0x9B, 0xE8, 0xFF)), 52 + col, 58);
                    c.DrawString("TEXT", title, new SolidBrush(ColorF.FromRgb(0xD8, 0xB2, 0xFF)), 62 + col * 2, 58);
                    c.DrawEllipse(new Pen(ColorF.FromRgb(0xFF, 0xA7, 0x63), 4), 68, 132, col - 80, 180);
                    c.FillRoundedRectangle(new SolidBrush(C(0x60, 0xB7, 0xFF, 150)), 62 + col, 132, col - 80, 180, 20);
                    c.DrawString("Aa Bb 0123\n🙂 👍 🇬🇧", new FontSpec(emojiFamily, 26), new SolidBrush(ColorF.FromRgb(0xF4, 0xE8, 0xFF)), 72 + col * 2, 150);
                }))
        );
    }
}

