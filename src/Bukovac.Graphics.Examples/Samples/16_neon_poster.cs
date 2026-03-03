using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_16(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("16-neon-poster", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x0E, 0x08, 0x16));
                    c.FillRoundedRectangle(new SolidBrush(C(0x24, 0x12, 0x3D, 255)), 28, 28, w - 56, h - 56, 22);
                    c.DrawRoundedRectangle(new Pen(ColorF.FromRgb(0x7B, 0xE6, 0xFF), 3), 28, 28, w - 56, h - 56, 22);
                    c.FillEllipse(new SolidBrush(C(0xFF, 0x4F, 0xB8, 130)), 90, 120, 280, 220);
                    c.FillEllipse(new SolidBrush(C(0x6D, 0xFF, 0xD0, 130)), 260, 90, 300, 250);
                    c.FillEllipse(new SolidBrush(C(0x7A, 0xA0, 0xFF, 130)), 470, 150, 220, 190);
                    c.DrawString("BUKOVAC", new FontSpec(uiFamily, 42, FontWeight.Bold), new SolidBrush(ColorF.FromRgb(0xFF, 0xF6, 0xDA)), 56, 58);
                    c.DrawString("Graphics Neon Poster", ui, new SolidBrush(ColorF.FromRgb(0xD6, 0xEB, 0xFF)), 60, 114);
                }))
        );
    }
}

