using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_13(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("13-alpha-overlap", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x0E, 0x0E, 0x0E));
                    c.FillEllipse(new SolidBrush(C(0xFF, 0x55, 0x55, 160)), 120, 90, 210, 210);
                    c.FillEllipse(new SolidBrush(C(0x55, 0xFF, 0x88, 160)), 240, 90, 210, 210);
                    c.FillEllipse(new SolidBrush(C(0x55, 0x88, 0xFF, 160)), 180, 190, 210, 210);
                    c.DrawString("Alpha blending overlap", ui, new SolidBrush(ColorF.White), 40, 30);
                }))
        );
    }
}

