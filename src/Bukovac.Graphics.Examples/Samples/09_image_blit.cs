using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_09(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("09-image-blit", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x1B, 0x11, 0x11));
                    int iw = 160;
                    int ih = 160;
                    byte[] px = new byte[iw * ih * 4];
                    for (int y = 0; y < ih; y++)
                    {
                        for (int x = 0; x < iw; x++)
                        {
                            bool odd = ((x / 20) + (y / 20)) % 2 == 0;
                            byte r = odd ? (byte)0xF4 : (byte)0x6A;
                            byte g = odd ? (byte)0x8B : (byte)0xD6;
                            byte b = odd ? (byte)0x57 : (byte)0xF0;
                            int i = ((y * iw) + x) * 4;
                            px[i + 0] = b;
                            px[i + 1] = g;
                            px[i + 2] = r;
                            px[i + 3] = 255;
                        }
                    }
            
                    ImageHandle img = c.LoadImage(iw, ih, px);
                    c.DrawImage(img, new RectF(60, 70, 220, 220));
                    c.DrawImage(img, new RectF(330, 70, 220, 220), new RectF(20, 20, 120, 120), 0.85f);
                    c.DrawString("Image draw + source crop", ui, new SolidBrush(ColorF.White), 60, 320);
                }))
        );
    }
}

