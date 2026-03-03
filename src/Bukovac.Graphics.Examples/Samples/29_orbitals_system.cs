using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_29(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("29-orbitals-system", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x08, 0x0D, 0x17));
                    c.DrawString("Orbital Dynamics", title, new SolidBrush(ColorF.FromRgb(0xE6, 0xEE, 0xFF)), 34, 28);
                    float cx = w * 0.5f;
                    float cy = h * 0.55f;
                    for (int i = 0; i < 13; i++)
                    {
                        float rx = 70 + i * 28;
                        float ry = rx * (0.52f + ((i % 4) * 0.11f));
                        c.DrawEllipse(new Pen(ColorF.FromArgb(70, (byte)(110 + i * 8), (byte)(140 + i * 6), (byte)255), 1), cx - rx, cy - ry, rx * 2, ry * 2);
                        float a = i * 0.43f;
                        float px = cx + rx * MathF.Cos(a);
                        float py = cy + ry * MathF.Sin(a);
                        c.FillEllipse(new SolidBrush(C((byte)(140 + i * 8), (byte)(180 - i * 5), (byte)(220 - i * 7), 210)), px - 4, py - 4, 8, 8);
                    }
                    c.FillEllipse(new SolidBrush(C(0xFF, 0xE2, 0x8A, 255)), cx - 14, cy - 14, 28, 28);
                }))
        );
    }
}

