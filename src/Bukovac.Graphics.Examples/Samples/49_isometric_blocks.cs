using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_49(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("49-isometric-blocks", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x0D, 0x12, 0x1B));
                    c.DrawString("Isometric Blocks", title, new SolidBrush(ColorF.FromRgb(0xE8, 0xF0, 0xFF)), 34, 26);

                    float ox = w * 0.5f;
                    float oy = h * 0.68f;
                    float sx = 24f;
                    float sy = 14f;
                    for (int gy = 0; gy < 8; gy++)
                    {
                        for (int gx = 0; gx < 11; gx++)
                        {
                            float h0 = 8f + ((gx * 13 + gy * 7) % 5) * 16f;
                            float cx = ox + (gx - gy) * sx;
                            float cy = oy + (gx + gy) * sy * 0.5f;
                            PointF top = new(cx, cy - h0);
                            PointF left = new(cx - sx, cy - sy - h0 * 0.5f);
                            PointF right = new(cx + sx, cy - sy - h0 * 0.5f);
                            PointF bl = new(cx - sx, cy + sy - h0 * 0.5f);
                            PointF br = new(cx + sx, cy + sy - h0 * 0.5f);

                            c.FillPolygon(new SolidBrush(ColorF.FromArgb(220, 0x86, 0xBE, 0xFF)), [top, right, br, bl, left]);
                            c.DrawPolygon(new Pen(ColorF.FromArgb(200, 0xD7, 0xF0, 0xFF), 1), [top, right, br, bl, left]);
                        }
                    }
                }))
        );
    }
}

