using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_46(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("46-spiral-bloom", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x0E, 0x0A, 0x12));
                    c.DrawString("Spiral Bloom", title, new SolidBrush(ColorF.FromRgb(0xF5, 0xE8, 0xFF)), 34, 26);

                    float cx = w * 0.5f;
                    float cy = h * 0.56f;
                    for (int i = 0; i < 1800; i++)
                    {
                        float t = i * 0.032f;
                        float r = 8f + i * 0.17f;
                        float x = cx + MathF.Cos(t) * r;
                        float y = cy + MathF.Sin(t * 1.07f) * r * 0.63f;
                        byte rr = (byte)(128 + 127 * MathF.Sin(t * 0.19f));
                        byte gg = (byte)(128 + 127 * MathF.Sin(t * 0.13f + 2.1f));
                        byte bb = (byte)(128 + 127 * MathF.Sin(t * 0.11f + 4.2f));
                        c.FillEllipse(new SolidBrush(ColorF.FromArgb(140, rr, gg, bb)), x, y, 2.5f, 2.5f);
                    }
                }))
        );
    }
}

