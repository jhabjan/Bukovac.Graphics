using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_12(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("12-dash-styles", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x13, 0x1A, 0x28));
                    c.FillRoundedRectangle(new SolidBrush(C(0x21, 0x2C, 0x44, 220)), 28, 28, w - 56, h - 56, 16);
                    c.DrawRoundedRectangle(new Pen(ColorF.FromRgb(0x90, 0xA9, 0xD7), 2), 28, 28, w - 56, h - 56, 16);
                    c.DrawString("Dash Style Matrix", title, new SolidBrush(ColorF.White), 44, 44);
                    c.DrawString("Labels + all enum values", ui, new SolidBrush(ColorF.FromRgb(0xC8, 0xDA, 0xFF)), 44, 82);
            
                    var rows = new (string Label, DashStyle Style, ColorF Color, float Width)[]
                    {
                        // Keep width at 1px for cross-rasterizer parity:
                        // GDI's classic dashed pen styles are effectively cosmetic at thicker widths.
                        ("Solid", DashStyle.Solid, ColorF.FromRgb(0xFF, 0xCA, 0x6E), 1f),
                        ("Dash", DashStyle.Dash, ColorF.FromRgb(0x9E, 0xD8, 0xFF), 1f),
                        ("Dot", DashStyle.Dot, ColorF.FromRgb(0xA5, 0xF0, 0xBC), 1f),
                        ("DashDot", DashStyle.DashDot, ColorF.FromRgb(0xFF, 0x92, 0xB2), 1f),
                        ("DashDotDot", DashStyle.DashDotDot, ColorF.FromRgb(0xD9, 0xB0, 0xFF), 1f),
                    };
            
                    float y = 140f;
                    foreach (var row in rows)
                    {
                        c.DrawString(row.Label, ui, new SolidBrush(ColorF.FromRgb(0xEB, 0xF2, 0xFF)), 46, y - 12);
                        c.DrawLine(new Pen(row.Color, row.Width) { DashStyle = row.Style }, 250, y, w - 60, y);
                        y += 68f;
                    }
                }))
        );
    }
}

