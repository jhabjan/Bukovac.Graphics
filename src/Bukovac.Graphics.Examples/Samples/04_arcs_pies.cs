using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_04(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("04-arcs-pies", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x15, 0x1A, 0x2E));
                    c.DrawArc(new Pen(ColorF.FromRgb(0x7A, 0xC9, 0xFF), 5), 40, 40, 220, 220, -30, 260);
                    c.DrawPie(new Pen(ColorF.FromRgb(0xFF, 0x7A, 0x9E), 4), 300, 40, 220, 220, 15, 110);
                    c.FillPie(new SolidBrush(C(0xFF, 0x7A, 0x9E, 160)), 300, 40, 220, 220, 15, 110);
                }))
        );
    }
}

