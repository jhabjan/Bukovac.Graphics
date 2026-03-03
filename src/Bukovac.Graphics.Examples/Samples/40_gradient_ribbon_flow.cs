using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_40(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("40-gradient-ribbon-flow", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x0D, 0x10, 0x17));
                    c.FillRoundedRectangle(new SolidBrush(C(0x1A, 0x1F, 0x2C, 255)), 20, 20, w - 40, h - 40, 16);
                    c.DrawString("Gradient Ribbon Flow", title, new SolidBrush(ColorF.FromRgb(0xE8, 0xF0, 0xFF)), 34, 26);
                    for (int band = 0; band < 20; band++)
                    {
                        float yBase = 86 + band * 20;
                        for (int x = 24; x < w - 24; x += 3)
                        {
                            float t = x / (float)Math.Max(1, w - 1);
                            float wave = MathF.Sin(t * 8.6f + band * 0.37f) * (8 + band * 0.6f);
                            byte r = (byte)(80 + 140 * t);
                            byte g = (byte)(70 + 120 * (1f - t));
                            byte b = (byte)(140 + 100 * MathF.Sin(t * 3.14f));
                            c.DrawLine(new Pen(ColorF.FromArgb(125, r, g, b), 2), x, yBase + wave, x + 3, yBase + wave);
                        }
                    }
                }))
        );
    }
}

