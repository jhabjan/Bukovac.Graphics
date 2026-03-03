using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_23(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("23-spirograph-lattice", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x12, 0x0A, 0x1A));
                    c.FillRoundedRectangle(new SolidBrush(C(0x23, 0x12, 0x32, 255)), 20, 20, w - 40, h - 40, 18);
                    c.DrawString("Spirograph Lattice", title, new SolidBrush(ColorF.FromRgb(0xFF, 0xE6, 0xFF)), 32, 26);
                    float cx = w * 0.5f;
                    float cy = h * 0.56f;
                    float r1 = MathF.Min(w, h) * 0.22f;
                    float r2 = r1 * 0.46f;
                    float p = r2 * 0.88f;
                    PointF prev = new(cx, cy);
                    for (int i = 1; i <= 7200; i++)
                    {
                        float t = i * 0.018f;
                        float x = (r1 - r2) * MathF.Cos(t) + p * MathF.Cos(((r1 - r2) / r2) * t);
                        float y = (r1 - r2) * MathF.Sin(t) - p * MathF.Sin(((r1 - r2) / r2) * t);
                        PointF cur = new(cx + x, cy + y);
                        byte r = (byte)(130 + (100 * MathF.Sin(t * 0.7f) + 100) * 0.5f);
                        byte g = (byte)(90 + (100 * MathF.Sin(t * 0.9f + 1.2f) + 100) * 0.5f);
                        byte b = (byte)(140 + (90 * MathF.Sin(t * 1.1f + 2.2f) + 90) * 0.5f);
                        c.DrawLine(new Pen(ColorF.FromArgb(90, r, g, b), 1), prev.X, prev.Y, cur.X, cur.Y);
                        prev = cur;
                    }
                }))
        );
    }
}

