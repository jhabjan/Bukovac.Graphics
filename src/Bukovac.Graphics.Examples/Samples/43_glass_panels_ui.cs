using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_43(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("43-glass-panels-ui", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x0A, 0x12, 0x1B));
                    c.FillEllipse(new SolidBrush(C(0x1A, 0xB4, 0xFF, 90)), 80, 60, 360, 240);
                    c.FillEllipse(new SolidBrush(C(0xFF, 0x57, 0x9F, 90)), 380, 120, 360, 260);
                    c.DrawString("Glass Panels", title, new SolidBrush(ColorF.FromRgb(0xEE, 0xF5, 0xFF)), 34, 26);

                    RectF left = new(70, 96, 360, 360);
                    RectF right = new(450, 110, 420, 330);
                    c.FillRoundedRectangle(new SolidBrush(C(0xD2, 0xE7, 0xFF, 36)), left, 18);
                    c.FillRoundedRectangle(new SolidBrush(C(0xFF, 0xEC, 0xF5, 30)), right, 18);
                    c.DrawRoundedRectangle(new Pen(ColorF.FromArgb(120, 0xEE, 0xF5, 0xFF), 2), left, 18);
                    c.DrawRoundedRectangle(new Pen(ColorF.FromArgb(120, 0xFF, 0xE8, 0xF4), 2), right, 18);

                    c.DrawString("System Health", ui, new SolidBrush(ColorF.FromRgb(0xE8, 0xF3, 0xFF)), left.X + 22, left.Y + 20);
                    c.DrawString("Render Queue", ui, new SolidBrush(ColorF.FromRgb(0xFF, 0xEE, 0xF8)), right.X + 22, right.Y + 20);

                    for (int i = 0; i < 8; i++)
                    {
                        float y = left.Y + 62 + i * 34;
                        float v = 0.3f + 0.7f * ((i + 1) / 8f);
                        c.FillRoundedRectangle(new SolidBrush(C(0x2D, 0x3F, 0x55, 180)), left.X + 20, y, 260, 18, 8);
                        c.FillRoundedRectangle(new SolidBrush(C((byte)(70 + v * 120), (byte)(140 + v * 90), (byte)(200 + v * 40), 210)), left.X + 20, y, 30 + i * 28, 18, 8);
                    }
                }))
        );
    }
}

