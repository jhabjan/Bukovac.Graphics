using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_32(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("32-3d-realistic-terrain", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x07, 0x0E, 0x17));
                    int fw = Math.Min(780, w - 70);
                    int fh = Math.Min(440, h - 100);
                    byte[] px = RenderRealisticTerrainBgra(fw, fh);
                    ImageHandle img = c.LoadImage(fw, fh, px);
                    c.DrawImage(img, new RectF(35, 50, fw, fh));
                    c.DrawRoundedRectangle(new Pen(ColorF.FromRgb(0xA1, 0xBF, 0xD9), 2), 35, 50, fw, fh, 10);
                    c.DrawString("3D Realistic Terrain", title, new SolidBrush(ColorF.FromRgb(0xE8, 0xF2, 0xFF)), 42, 14);
                }))
        );
    }
}

