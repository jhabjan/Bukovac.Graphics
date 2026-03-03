using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_19(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("19-fractal-sierpinski", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x0E, 0x10, 0x1B));
                    c.FillRoundedRectangle(new SolidBrush(C(0x18, 0x22, 0x39, 255)), 20, 20, w - 40, h - 40, 16);
                    c.DrawString("Fractal: Sierpinski Triangle", title, new SolidBrush(ColorF.FromRgb(0xF3, 0xF9, 0xFF)), 34, 34);
                    c.DrawString("Depth 8 recursive subdivision", ui, new SolidBrush(ColorF.FromRgb(0xB8, 0xD8, 0xFF)), 36, 72);
            
                    var a = new PointF(w * 0.5f, 108);
                    var b = new PointF(110, h - 58);
                    var d = new PointF(w - 110, h - 58);
                    DrawSierpinski(c, a, b, d, 8, ColorF.FromRgb(0x7D, 0xE8, 0xFF));
                    c.DrawPolygon(new Pen(ColorF.FromRgb(0xE6, 0xF4, 0xFF), 1), [a, b, d]);
                }))
        );
    }
}

