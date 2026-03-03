using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_34(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("34-nebula-clouds", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x08, 0x05, 0x12));
                    int fw = Math.Min(780, w - 70);
                    int fh = Math.Min(440, h - 100);
                    byte[] px = RenderNebulaBgra(fw, fh);
                    ImageHandle img = c.LoadImage(fw, fh, px);
                    c.DrawImage(img, new RectF(35, 50, fw, fh));
                    c.DrawRoundedRectangle(new Pen(ColorF.FromRgb(0xC6, 0xAB, 0xFF), 2), 35, 50, fw, fh, 10);
                    c.DrawString("Nebula Volumes", title, new SolidBrush(ColorF.FromRgb(0xF2, 0xE8, 0xFF)), 42, 14);
                }))
        );
    }
}

