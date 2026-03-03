using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_33(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("33-space-starfield", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x04, 0x07, 0x12));
                    int fw = Math.Min(780, w - 70);
                    int fh = Math.Min(440, h - 100);
                    byte[] px = RenderSpaceStarfieldBgra(fw, fh);
                    ImageHandle img = c.LoadImage(fw, fh, px);
                    c.DrawImage(img, new RectF(35, 50, fw, fh));
                    c.DrawRoundedRectangle(new Pen(ColorF.FromRgb(0x9A, 0xB6, 0xE8), 2), 35, 50, fw, fh, 10);
                    c.DrawString("Deep Space Starfield", title, new SolidBrush(ColorF.FromRgb(0xE9, 0xF1, 0xFF)), 42, 14);
                }))
        );
    }
}

