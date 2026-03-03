using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_08(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("08-clipping-transform", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x0E, 0x1A, 0x18));
                    c.Save();
                    c.SetClip(new RectF(90, 80, w - 180, h - 160));
                    c.FillRectangle(new SolidBrush(C(0x36, 0x4D, 0x6A, 180)), 90, 80, w - 180, h - 160);
                    c.SetTransform(System.Numerics.Matrix3x2.CreateRotation(0.35f, new System.Numerics.Vector2(w / 2f, h / 2f)));
                    c.FillRoundedRectangle(new SolidBrush(C(0x88, 0xF0, 0xE0, 180)), 150, 120, w - 300, h - 240, 22);
                    c.DrawString("Rotated and clipped", title, new SolidBrush(ColorF.FromRgb(0xE9, 0xFF, 0xFA)), 170, h / 2f - 20);
                    c.Restore();
                    c.DrawRectangle(new Pen(ColorF.FromRgb(0x8A, 0xB4, 0xC8), 2), 90, 80, w - 180, h - 160);
                }))
        );
    }
}

