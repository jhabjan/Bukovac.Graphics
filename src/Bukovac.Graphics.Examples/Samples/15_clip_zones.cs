using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_15(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("15-clip-zones", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x16, 0x1F, 0x16));
                    c.FillRectangle(new SolidBrush(C(0x2A, 0x40, 0x2A, 255)), 20, 20, w - 40, h - 40);
                    c.Save();
                    c.SetClip(new RectF(40, 50, (w / 2f) - 50, h - 100));
                    for (int i = 0; i < 18; i++)
                        c.DrawLine(new Pen(ColorF.FromRgb(0xB0, 0xFF, 0xC2), 2), 20, 40 + (i * 20), w - 20, 80 + (i * 20));
                    c.Restore();
                    c.Save();
                    c.SetClip(new RectF((w / 2f) + 10, 50, (w / 2f) - 50, h - 100));
                    for (int i = 0; i < 18; i++)
                        c.DrawLine(new Pen(ColorF.FromRgb(0x99, 0xC6, 0xFF), 2), 20, 360 - (i * 18), w - 20, 80 + (i * 20));
                    c.Restore();
                    c.DrawRectangle(new Pen(ColorF.FromRgb(0xE5, 0xF7, 0xE5), 2), 40, 50, (w / 2f) - 50, h - 100);
                    c.DrawRectangle(new Pen(ColorF.FromRgb(0xD6, 0xE8, 0xFF), 2), (w / 2f) + 10, 50, (w / 2f) - 50, h - 100);
                }))
        );
    }
}

