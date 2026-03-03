using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_38(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("38-3d-isometric-city", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x10, 0x14, 0x1E));
                    c.FillRoundedRectangle(new SolidBrush(C(0x19, 0x23, 0x32, 255)), 20, 20, w - 40, h - 40, 16);
                    c.DrawString("3D Isometric City", title, new SolidBrush(ColorF.FromRgb(0xE9, 0xF2, 0xFF)), 34, 26);
                    float cx = w * 0.5f;
                    float cy = h * 0.72f;
                    for (int gz = 0; gz < 7; gz++)
                    {
                        for (int gx = 0; gx < 10; gx++)
                        {
                            float bx = (gx - 4.5f) * 52f;
                            float bz = (gz - 3.0f) * 44f;
                            float hh = 30 + ((gx * 19 + gz * 27) % 7) * 22;
                            var baseColor = ColorF.FromRgb((byte)(90 + gx * 9), (byte)(120 + gz * 10), (byte)(170 + (gx + gz) * 4));
                            DrawIsoBox(c, cx, cy, bx, bz, 34f, 28f, hh, baseColor);
                        }
                    }
                }))
        );
    }
}

