using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_48(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("48-metaball-lights", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x07, 0x0E, 0x18));
                    c.DrawString("Metaball Lights", title, new SolidBrush(ColorF.FromRgb(0xE9, 0xF2, 0xFF)), 34, 26);

                    for (int i = 0; i < 9; i++)
                    {
                        float t = i / 8f;
                        float cx = 110 + t * (w - 220) + MathF.Sin(i * 1.7f) * 28f;
                        float cy = h * 0.56f + MathF.Cos(i * 0.9f) * 70f;
                        float r = 120 - i * 7;
                        byte rr = (byte)(160 + 95 * MathF.Sin(i * 0.7f));
                        byte gg = (byte)(140 + 110 * MathF.Sin(i * 1.2f + 1f));
                        byte bb = (byte)(170 + 85 * MathF.Sin(i * 1.5f + 2f));
                        c.FillEllipse(new SolidBrush(ColorF.FromArgb(55, rr, gg, bb)), cx - r, cy - r, r * 2, r * 2);
                        c.FillEllipse(new SolidBrush(ColorF.FromArgb(120, rr, gg, bb)), cx - 16, cy - 16, 32, 32);
                    }
                }))
        );
    }
}

