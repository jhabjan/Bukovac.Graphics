using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_26(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("26-julia-zoom", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x05, 0x07, 0x0C));
                    int fw = Math.Min(760, w - 80);
                    int fh = Math.Min(420, h - 120);
                    byte[] px = RenderJuliaSetBgra(fw, fh, -0.770, 0.115, 160);
                    ImageHandle img = c.LoadImage(fw, fh, px);
                    c.DrawImage(img, new RectF(40, 70, fw, fh));
                    c.DrawRoundedRectangle(new Pen(ColorF.FromRgb(0xC0, 0xD7, 0xFF), 2), 40, 70, fw, fh, 8);
                    c.DrawString("Julia Set Detail", title, new SolidBrush(ColorF.FromRgb(0xF0, 0xF6, 0xFF)), 44, 28);
                }))
        );
    }
}

