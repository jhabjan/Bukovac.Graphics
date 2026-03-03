using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_28(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("28-polar-harmonics", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x15, 0x0D, 0x1A));
                    c.FillRoundedRectangle(new SolidBrush(C(0x28, 0x17, 0x35, 255)), 20, 20, w - 40, h - 40, 18);
                    c.DrawString("Polar Harmonics", title, new SolidBrush(ColorF.FromRgb(0xFF, 0xEF, 0xFF)), 34, 28);
                    float cx = w * 0.5f;
                    float cy = h * 0.56f;
                    for (int layer = 0; layer < 7; layer++)
                    {
                        float scale = 44 + layer * 32;
                        PointF prev = new(cx, cy);
                        for (int i = 1; i <= 1600; i++)
                        {
                            float t = i * 0.010f;
                            float r = scale * (0.9f + 0.26f * MathF.Sin((6 + layer) * t) + 0.18f * MathF.Cos((3 + layer) * t * 1.7f));
                            PointF cur = new(cx + (r * MathF.Cos(t)), cy + (r * MathF.Sin(t)));
                            byte rr = (byte)(120 + layer * 14);
                            byte gg = (byte)(70 + (i % 140));
                            byte bb = (byte)(180 + layer * 8);
                            c.DrawLine(new Pen(ColorF.FromArgb(55, rr, gg, bb), 1), prev.X, prev.Y, cur.X, cur.Y);
                            prev = cur;
                        }
                    }
                }))
        );
    }
}

