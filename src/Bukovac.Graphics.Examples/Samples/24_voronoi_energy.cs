using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_24(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("24-voronoi-energy", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x06, 0x09, 0x13));
                    int fw = Math.Min(760, w - 80);
                    int fh = Math.Min(420, h - 120);
                    byte[] px = RenderVoronoiBgra(fw, fh, 26);
                    ImageHandle img = c.LoadImage(fw, fh, px);
                    c.DrawImage(img, new RectF(40, 70, fw, fh));
                    c.DrawRoundedRectangle(new Pen(ColorF.FromRgb(0xA7, 0xC4, 0xFF), 2), 40, 70, fw, fh, 8);
                    c.DrawString("Voronoi Energy Field", title, new SolidBrush(ColorF.FromRgb(0xE8, 0xF1, 0xFF)), 44, 28);
                }))
        );
    }
}

