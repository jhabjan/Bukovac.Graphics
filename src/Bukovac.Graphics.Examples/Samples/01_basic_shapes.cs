using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_01(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("01-basic-shapes", (Action<Canvas, int, int>)((c, w, h) =>
            {
                c.Clear(ColorF.FromRgb(0x12, 0x16, 0x22));
                c.DrawLine(new Pen(ColorF.FromRgb(0xFC, 0xD3, 0x4D), 3), 40, 40, w - 40, 100);
                c.DrawRectangle(new Pen(ColorF.FromRgb(0x7D, 0xE3, 0x7A), 2), 40, 130, 260, 120);
                c.FillRectangle(new SolidBrush(C(0x7D, 0xE3, 0x7A, 60)), 50, 140, 240, 100);
                c.DrawString("Basic lines/rectangles", ui, new SolidBrush(ColorF.White), 40, 275);
            }))
        );
    }
}

