using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_11(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("11-wordwrap-measure", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x16, 0x14, 0x20));
                    string block = "Bukovac.Graphics measurement and wrapping example. " +
                                   "This sample intentionally draws a long sentence into a bounded rectangle.";
                    RectF rect = new(50, 70, w - 100, 260);
                    c.FillRoundedRectangle(new SolidBrush(C(0x45, 0x37, 0x70, 180)), rect, 14);
                    c.DrawString(block, ui, new SolidBrush(ColorF.FromRgb(0xFF, 0xF4, 0xD4)), rect, TextAlignment.Near, TextFormatFlags.None);
                    var measured = c.MeasureString(block, ui, TextFormatFlags.None, rect.Width);
                    c.DrawString($"Measured: {measured.X:F1} x {measured.Y:F1}", ui, new SolidBrush(ColorF.White), 50, 350);
                }))
        );
    }
}

