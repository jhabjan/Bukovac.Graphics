using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_35(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("35-3d-text-poster", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x0E, 0x14, 0x1B));
                    c.FillRoundedRectangle(new SolidBrush(C(0x17, 0x24, 0x31, 255)), 20, 20, w - 40, h - 40, 16);
                    c.FillEllipse(new SolidBrush(C(0x66, 0xB8, 0xFF, 80)), 70, 80, 320, 260);
                    c.FillEllipse(new SolidBrush(C(0xFF, 0xA8, 0x66, 80)), 330, 120, 360, 280);
                    var heavy = new FontSpec(uiFamily, 92, FontWeight.Black);
                    string text = "BAS";
                    float x = 130;
                    float y = 180;
                    for (int i = 16; i >= 1; i--)
                    {
                        byte shade = (byte)(24 + i * 7);
                        c.DrawString(text, heavy, new SolidBrush(ColorF.FromRgb(shade, (byte)(shade + 12), (byte)(shade + 22))), x + i * 2.2f, y + i * 2.0f);
                    }
                    c.DrawString(text, heavy, new SolidBrush(ColorF.FromRgb(0xF5, 0xFA, 0xFF)), x, y);
                    c.DrawString("3D Text Poster", title, new SolidBrush(ColorF.FromRgb(0xE2, 0xEE, 0xFF)), 36, 34);
                }))
        );
    }
}

