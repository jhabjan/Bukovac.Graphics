using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_39(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("39-gradient-sunrise", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x0B, 0x0D, 0x14));
                    int fw = Math.Min(780, w - 70);
                    int fh = Math.Min(440, h - 100);
                    byte[] px = RenderGradientSunriseBgra(fw, fh);
                    ImageHandle img = c.LoadImage(fw, fh, px);
                    c.DrawImage(img, new RectF(35, 50, fw, fh));
                    c.DrawRoundedRectangle(new Pen(ColorF.FromRgb(0xE2, 0xC3, 0xA3), 2), 35, 50, fw, fh, 10);
                    c.DrawString("Gradient Sunrise", title, new SolidBrush(ColorF.FromRgb(0xFF, 0xF1, 0xE0)), 42, 14);
                }))
        );
    }
}

