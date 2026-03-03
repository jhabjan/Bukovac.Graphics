using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_42(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("42-neon-tunnel-grid", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x07, 0x09, 0x12));
                    c.DrawString("Neon Tunnel Grid", title, new SolidBrush(ColorF.FromRgb(0xD8, 0xF0, 0xFF)), 34, 26);

                    float cx = w * 0.5f;
                    float cy = h * 0.58f;
                    for (int i = 1; i <= 28; i++)
                    {
                        float t = i / 28f;
                        float z = 1f / (0.08f + t);
                        float rw = 42f * z;
                        float rh = 20f * z;
                        byte a = (byte)(40 + (180 * (1f - t)));
                        c.DrawRectangle(new Pen(ColorF.FromArgb(a, 0x73, 0xD8, 0xFF), 2), cx - rw, cy - rh, rw * 2f, rh * 2f);
                    }

                    for (int line = -14; line <= 14; line++)
                    {
                        float fx = cx + line * 22f;
                        c.DrawLine(new Pen(ColorF.FromArgb(130, 0x8A, 0x4D, 0xFF), 1), fx, h, cx + line * 6f, cy + 1);
                    }
                }))
        );
    }
}

