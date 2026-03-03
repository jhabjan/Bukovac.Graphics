using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_20(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("20-fractal-koch-snowflake", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x08, 0x1A, 0x22));
                    c.FillRoundedRectangle(new SolidBrush(C(0x12, 0x2C, 0x38, 255)), 22, 22, w - 44, h - 44, 16);
                    c.DrawString("Fractal: Koch Snowflake", title, new SolidBrush(ColorF.FromRgb(0xE8, 0xF8, 0xFF)), 34, 34);
                    c.DrawString("Depth 5 edge refinement", ui, new SolidBrush(ColorF.FromRgb(0xA6, 0xE2, 0xFF)), 36, 72);
            
                    float radius = MathF.Min(w, h) * 0.33f;
                    float cx = w * 0.5f;
                    float cy = h * 0.58f;
                    PointF p1 = new(cx, cy - radius);
                    PointF p2 = new(cx - radius * 0.8660254f, cy + radius * 0.5f);
                    PointF p3 = new(cx + radius * 0.8660254f, cy + radius * 0.5f);
            
                    var glow = new Pen(ColorF.FromArgb(130, 0x74, 0xD6, 0xFF), 3);
                    var main = new Pen(ColorF.FromRgb(0xD6, 0xF4, 0xFF), 1);
                    DrawKochEdge(c, glow, p1, p2, 5);
                    DrawKochEdge(c, glow, p2, p3, 5);
                    DrawKochEdge(c, glow, p3, p1, 5);
                    DrawKochEdge(c, main, p1, p2, 5);
                    DrawKochEdge(c, main, p2, p3, 5);
                    DrawKochEdge(c, main, p3, p1, 5);
                }))
        );
    }
}

