using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_45(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("45-blueprint-schematic", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x0A, 0x19, 0x34));
                    c.DrawString("Blueprint Schematic", title, new SolidBrush(ColorF.FromRgb(0xD8, 0xEB, 0xFF)), 34, 26);

                    for (int x = 20; x < w; x += 24)
                    {
                        c.DrawLine(new Pen(ColorF.FromArgb(70, 0x6A, 0x92, 0xBF), 1) { RenderMode = StrokeRenderMode.AlphaAccurate }, x, 20, x, h - 20);
                    }

                    for (int y = 20; y < h; y += 24)
                    {
                        c.DrawLine(new Pen(ColorF.FromArgb(70, 0x6A, 0x92, 0xBF), 1) { RenderMode = StrokeRenderMode.AlphaAccurate }, 20, y, w - 20, y);
                    }

                    RectF chassis = new(90, 120, 620, 280);
                    c.DrawRoundedRectangle(new Pen(ColorF.FromRgb(0xD4, 0xEC, 0xFF), 3), chassis, 10);
                    c.DrawRectangle(new Pen(ColorF.FromArgb(200, 0xBF, 0xDE, 0xFF), 2), 130, 160, 210, 88);
                    c.DrawRectangle(new Pen(ColorF.FromArgb(200, 0xBF, 0xDE, 0xFF), 2), 360, 160, 300, 88);
                    c.DrawEllipse(new Pen(ColorF.FromArgb(200, 0xBF, 0xDE, 0xFF), 2), 180, 276, 120, 120);
                    c.DrawEllipse(new Pen(ColorF.FromArgb(200, 0xBF, 0xDE, 0xFF), 2), 488, 276, 120, 120);
                }))
        );
    }
}
