using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_21(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("21-fractal-mandelbrot", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x05, 0x07, 0x10));
                    c.FillRoundedRectangle(new SolidBrush(C(0x0B, 0x10, 0x1D, 255)), 20, 20, w - 40, h - 40, 16);
                    c.DrawString("Fractal: Mandelbrot Set", title, new SolidBrush(ColorF.FromRgb(0xF0, 0xF5, 0xFF)), 34, 34);
                    c.DrawString("Escape-time shading, max iterations: 120", ui, new SolidBrush(ColorF.FromRgb(0xAB, 0xC6, 0xFF)), 36, 72);
            
                    int fw = Math.Min(640, w - 140);
                    int fh = Math.Min(360, h - 170);
                    byte[] px = RenderMandelbrotBgra(fw, fh, -0.72, 0.0, 1.2, 120);
                    ImageHandle fractal = c.LoadImage(fw, fh, px);
                    c.DrawImage(fractal, new RectF(70, 112, fw, fh));
                    c.DrawRectangle(new Pen(ColorF.FromRgb(0x86, 0xA2, 0xD2), 1), 70, 112, fw, fh);
                }))
        );
    }
}

