using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_30(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("30-topographic-contours", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x0C, 0x10, 0x14));
                    c.FillRoundedRectangle(new SolidBrush(C(0x15, 0x20, 0x2A, 255)), 20, 20, w - 40, h - 40, 16);
                    c.DrawString("Topographic Contours", title, new SolidBrush(ColorF.FromRgb(0xE7, 0xF4, 0xFF)), 34, 28);
                    float sx = 34;
                    float sy = 80;
                    float sw = w - 68;
                    float sh = h - 124;
                    for (int l = 0; l < 18; l++)
                    {
                        float iso = -0.9f + l * 0.11f;
                        byte g = (byte)(110 + l * 6);
                        var pen = new Pen(ColorF.FromArgb(130, (byte)110, g, (byte)190), 1);
                        for (int y = 0; y < 220; y++)
                        {
                            float yy = sy + (y / 219f) * sh;
                            float py = ((y / 219f) * 2f - 1f) * 2.1f;
                            float prevVal = FieldHeight(-2.8f, py) - iso;
                            float prevX = sx;
                            for (int x = 1; x < 360; x++)
                            {
                                float pxn = ((x / 359f) * 2f - 1f) * 2.8f;
                                float val = FieldHeight(pxn, py) - iso;
                                float xx = sx + (x / 359f) * sw;
                                if ((prevVal <= 0 && val > 0) || (prevVal >= 0 && val < 0))
                                    c.DrawLine(pen, prevX, yy, xx, yy);
                                prevVal = val;
                                prevX = xx;
                            }
                        }
                    }
                    c.DrawRoundedRectangle(new Pen(ColorF.FromRgb(0x89, 0xA8, 0xC4), 1), sx, sy, sw, sh, 10);
                }))
        );
    }
}

