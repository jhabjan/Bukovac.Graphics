using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_06(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("06-text-alignment", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x11, 0x18, 0x1F));
                    RectF box = new(50, 70, w - 100, 260);
                    c.FillRoundedRectangle(new SolidBrush(C(0x23, 0x35, 0x4A, 255)), box, 10);
                    c.DrawLine(new Pen(ColorF.FromRgb(0x6D, 0xC7, 0xFF), 1), box.X + 12, box.Y + 60, box.Right - 12, box.Y + 60);
                    c.DrawLine(new Pen(ColorF.FromRgb(0x6D, 0xC7, 0xFF), 1), box.X + 12, box.Y + 150, box.Right - 12, box.Y + 150);
                    c.DrawLine(new Pen(ColorF.FromRgb(0x6D, 0xC7, 0xFF), 1), box.X + 12, box.Y + 240, box.Right - 12, box.Y + 240);
                    c.DrawRectangle(new Pen(ColorF.FromRgb(0x4F, 0x63, 0x79), 2), box);
                    c.DrawString("Near aligned text", ui, new SolidBrush(ColorF.FromRgb(0xD0, 0xE8, 0xFF)),
                        new RectF(box.X + 12, box.Y + 20, box.Width - 24, 40), TextAlignment.Near, TextFormatFlags.NoWrap);
                    c.DrawString("Center aligned text", ui, new SolidBrush(ColorF.FromRgb(0xFF, 0xDB, 0x8A)),
                        new RectF(box.X + 12, box.Y + 110, box.Width - 24, 40), TextAlignment.Center, TextFormatFlags.NoWrap);
                    // GDI can clip the last baseline when the layout box sits exactly on the visual guide line.
                    c.DrawString("Far aligned text", ui, new SolidBrush(ColorF.FromRgb(0xAE, 0xF4, 0xC8)),
                        new RectF(box.X + 12, box.Y + 196, box.Width - 24, 40), TextAlignment.Far, TextFormatFlags.NoWrap);
                }))
        );
    }
}

