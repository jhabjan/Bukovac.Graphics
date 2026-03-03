using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_05(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("05-beziers", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x1D, 0x11, 0x1A));
                    c.FillRectangle(new SolidBrush(C(0x2D, 0x22, 0x3E, 255)), 20, 20, w - 40, h - 40);
                    c.DrawLine(new Pen(ColorF.FromRgb(0xFF, 0xC1, 0x6B), 2), 20, 20, w - 20, h - 20);
                    c.DrawLine(new Pen(ColorF.FromRgb(0x6B, 0xD6, 0xFF), 2), w - 20, 20, 20, h - 20);
                    var pen = new Pen(ColorF.FromRgb(0xD7, 0x8C, 0xFF), 3);
                    c.DrawBezier(pen, new(40, 300), new(120, 60), new(320, 520), new(420, 260));
                    c.DrawBezier(new Pen(ColorF.FromRgb(0x5E, 0xD6, 0xA2), 3),
                        new(240, 300), new(320, 40), new(520, 520), new(600, 220));
                    c.DrawString("Bezier curves", title, new SolidBrush(ColorF.White), 40, 20);
                }))
        );
    }
}

