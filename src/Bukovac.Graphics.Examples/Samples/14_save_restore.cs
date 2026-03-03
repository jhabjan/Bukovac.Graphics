using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_14(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("14-save-restore", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x1A, 0x14, 0x12));
                    c.FillRectangle(new SolidBrush(C(0x3C, 0x2A, 0x24, 255)), 30, 40, w - 60, h - 80);
                    c.Save();
                    c.SetTransform(System.Numerics.Matrix3x2.CreateRotation(-0.22f, new System.Numerics.Vector2(w / 2f, h / 2f)));
                    c.FillRoundedRectangle(new SolidBrush(C(0xFF, 0xA7, 0x6C, 180)), 170, 120, 380, 170, 24);
                    c.Restore();
                    c.DrawRoundedRectangle(new Pen(ColorF.FromRgb(0xFF, 0xD2, 0x9D), 3), 30, 40, w - 60, h - 80, 16);
                    c.DrawString("Save/Restore transform state", ui, new SolidBrush(ColorF.White), 40, 60);
                }))
        );
    }
}

