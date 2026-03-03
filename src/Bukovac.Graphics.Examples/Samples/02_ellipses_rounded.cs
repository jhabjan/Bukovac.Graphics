using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_02(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("02-ellipses-rounded", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x19, 0x13, 0x24));
                    c.DrawEllipse(new Pen(ColorF.FromRgb(0x89, 0xD2, 0xFF), 3), 40, 50, 220, 130);
                    c.FillEllipse(new SolidBrush(C(0x89, 0xD2, 0xFF, 90)), 300, 50, 220, 130);
                    c.DrawRoundedRectangle(new Pen(ColorF.FromRgb(0xFF, 0x8A, 0x65), 3), 40, 220, 220, 120, 24);
                    c.FillRoundedRectangle(new SolidBrush(C(0xFF, 0x8A, 0x65, 90)), 300, 220, 220, 120, 24);
                }))
        );
    }
}

