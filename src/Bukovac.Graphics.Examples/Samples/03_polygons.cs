using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_03(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("03-polygons", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x10, 0x1E, 0x1B));
                    PointF[] p1 =
                    [
                        new(70, 80), new(220, 40), new(300, 120), new(220, 200), new(90, 170),
                    ];
                    PointF[] p2 =
                    [
                        new(370, 80), new(510, 40), new(560, 170), new(460, 220), new(340, 180),
                    ];
                    c.FillPolygon(new SolidBrush(C(0x7A, 0xE5, 0xA8, 150)), p1);
                    c.DrawPolygon(new Pen(ColorF.FromRgb(0x7A, 0xE5, 0xA8), 3), p1);
                    c.FillPolygon(new SolidBrush(C(0xFF, 0xD1, 0x66, 150)), p2);
                    c.DrawPolygon(new Pen(ColorF.FromRgb(0xFF, 0xD1, 0x66), 3), p2);
                }))
        );
    }
}

