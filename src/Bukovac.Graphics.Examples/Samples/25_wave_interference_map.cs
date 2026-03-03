using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_25(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("25-wave-interference-map", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x0A, 0x0E, 0x15));
                    int fw = Math.Min(760, w - 80);
                    int fh = Math.Min(420, h - 120);
                    byte[] px = RenderWaveFieldBgra(fw, fh);
                    ImageHandle img = c.LoadImage(fw, fh, px);
                    c.DrawImage(img, new RectF(40, 70, fw, fh));
                    c.DrawRectangle(new Pen(ColorF.FromRgb(0x84, 0xA0, 0xCA), 1), 40, 70, fw, fh);
                    c.DrawString("Wave Interference Map", title, new SolidBrush(ColorF.FromRgb(0xEC, 0xF6, 0xFF)), 44, 28);
                }))
        );
    }
}

