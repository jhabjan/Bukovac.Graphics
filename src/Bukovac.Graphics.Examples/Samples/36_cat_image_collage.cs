using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_36(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("36-cat-image-collage", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x13, 0x10, 0x0D));
                    c.FillRoundedRectangle(new SolidBrush(C(0x24, 0x1D, 0x17, 255)), 20, 20, w - 40, h - 40, 16);
                    c.DrawString("Cat Image Collage", title, new SolidBrush(ColorF.FromRgb(0xFF, 0xF2, 0xE0)), 36, 30);
                    int iw = 220;
                    int ih = 160;
                    byte[] cat1 = RenderCatFaceBgra(iw, ih, 1);
                    byte[] cat2 = RenderCatFaceBgra(iw, ih, 2);
                    byte[] cat3 = RenderCatFaceBgra(iw, ih, 3);
                    ImageHandle i1 = c.LoadImage(iw, ih, cat1);
                    ImageHandle i2 = c.LoadImage(iw, ih, cat2);
                    ImageHandle i3 = c.LoadImage(iw, ih, cat3);
                    c.DrawImage(i1, new RectF(70, 100, 250, 190));
                    c.DrawImage(i2, new RectF(360, 86, 270, 205));
                    c.DrawImage(i3, new RectF(200, 290, 290, 210));
                    c.DrawRoundedRectangle(new Pen(ColorF.FromRgb(0xF4, 0xD2, 0xA9), 2), 70, 100, 250, 190, 10);
                    c.DrawRoundedRectangle(new Pen(ColorF.FromRgb(0xF4, 0xD2, 0xA9), 2), 360, 86, 270, 205, 10);
                    c.DrawRoundedRectangle(new Pen(ColorF.FromRgb(0xF4, 0xD2, 0xA9), 2), 200, 290, 290, 210, 10);
                }))
        );
    }
}

