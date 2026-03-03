using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_44(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("44-harmonic-topography", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x0B, 0x10, 0x14));
                    c.DrawString("Harmonic Topography", title, new SolidBrush(ColorF.FromRgb(0xE5, 0xF4, 0xE9)), 34, 26);
                    c.FillRoundedRectangle(new SolidBrush(C(0x14, 0x1F, 0x22, 255)), 20, 20, w - 40, h - 40, 14);

                    for (int band = 0; band < 26; band++)
                    {
                        float yBase = 90 + band * 14;
                        byte g = (byte)(120 + band * 4);
                        byte b = (byte)(80 + band * 2);
                        var pen = new Pen(ColorF.FromArgb(180, 120, g, b), 2);
                        for (int x = 26; x < w - 26; x += 4)
                        {
                            float u = x * 0.012f;
                            float y = yBase + MathF.Sin(u + band * 0.33f) * (8 + band * 0.45f) + MathF.Sin(u * 0.43f - band * 0.21f) * 12f;
                            float y2 = yBase + MathF.Sin((x + 4) * 0.012f + band * 0.33f) * (8 + band * 0.45f) + MathF.Sin((x + 4) * 0.012f * 0.43f - band * 0.21f) * 12f;
                            c.DrawLine(pen, x, y, x + 4, y2);
                        }
                    }
                }))
        );
    }
}

