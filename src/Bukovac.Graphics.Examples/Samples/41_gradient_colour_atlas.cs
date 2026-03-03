using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_41(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("41-gradient-colour-atlas", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x0A, 0x0E, 0x16));
                    c.FillRoundedRectangle(new SolidBrush(C(0x14, 0x1A, 0x26, 255)), 20, 20, w - 40, h - 40, 16);
                    c.DrawString("Gradient Colour Atlas", title, new SolidBrush(ColorF.FromRgb(0xF4, 0xF8, 0xFF)), 34, 26);

                    float x0 = 34f;
                    float y0 = 90f;
                    float ww = w - 68f;
                    float hh = h - 128f;

                    for (int y = 0; y < (int)hh; y += 2)
                    {
                        float v = y / MathF.Max(1f, hh - 1f);
                        for (int x = 0; x < (int)ww; x += 3)
                        {
                            float u = x / MathF.Max(1f, ww - 1f);
                            float r = 0.5f + 0.5f * MathF.Sin((u * 6.28f) + (v * 2.0f));
                            float g = 0.5f + 0.5f * MathF.Sin((u * 4.71f) + (v * 5.3f) + 1.1f);
                            float b = 0.5f + 0.5f * MathF.Sin((u * 9.2f) - (v * 3.7f) + 2.4f);
                            c.DrawLine(new Pen(ColorF.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255)), 2), x0 + x, y0 + y, x0 + x + 3, y0 + y);
                        }
                    }

                    float cx = w * 0.79f;
                    float cy = h * 0.36f;
                    for (int i = 0; i < 26; i++)
                    {
                        float t = i / 25f;
                        float rr = 170f - i * 5.8f;
                        byte r = (byte)(255 * (1f - t));
                        byte g = (byte)(180 + 60 * MathF.Sin(t * 6.28f));
                        byte b = (byte)(255 * t);
                        c.DrawEllipse(new Pen(ColorF.FromArgb(150, r, g, b), 3), cx - rr, cy - rr * 0.58f, rr * 2f, rr * 1.16f);
                    }
                }))
        );
    }
}
