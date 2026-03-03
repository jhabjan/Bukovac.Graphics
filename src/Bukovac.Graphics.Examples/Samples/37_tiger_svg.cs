using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_37(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("37-tiger-svg", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x0D, 0x0D, 0x0E));
                    RectF target = new(35, 50, Math.Min(780, w - 70), Math.Min(440, h - 100));
                    if (DrawSvgToCanvas(c, GhostscriptTigerSvgData.Content, target))
                    {
                        c.DrawRoundedRectangle(new Pen(ColorF.FromRgb(0xC8, 0xD5, 0xE8), 2), target, 10);
                        c.DrawString("Tiger (SVG)", title, new SolidBrush(ColorF.FromRgb(0xEE, 0xF4, 0xFF)), 42, 14);
                    }
                    else
                    {
                        c.FillRoundedRectangle(new SolidBrush(C(0x1E, 0x22, 0x28, 255)), target, 10);
                        c.DrawRoundedRectangle(new Pen(ColorF.FromRgb(0x7D, 0x8A, 0x9B), 2), target, 10);
                        c.DrawString("SVG render failed: embedded tiger SVG string", ui, new SolidBrush(ColorF.FromRgb(0xFF, 0xD6, 0xD6)), 48, 76);
                    }
                }))
        );
    }
}


