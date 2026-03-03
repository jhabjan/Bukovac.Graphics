using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_50(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("50-typography-poster", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x12, 0x0C, 0x0D));
                    c.FillRoundedRectangle(new SolidBrush(C(0x1D, 0x14, 0x15, 255)), 22, 22, w - 44, h - 44, 16);
                    c.DrawString("TYPOGRAPHY", new FontSpec(uiFamily, 70, FontWeight.Bold), new SolidBrush(ColorF.FromRgb(0xF8, 0xEB, 0xD6)), 34, 36);
                    c.DrawString("Poster Composition", title, new SolidBrush(ColorF.FromRgb(0xFF, 0xB1, 0x8A)), 36, 116);

                    string line = "B U K O V A C";
                    for (int i = 0; i < 18; i++)
                    {
                        float y = 170 + i * 18;
                        byte a = (byte)(45 + i * 8);
                        c.DrawString(
                            line,
                            new FontSpec(monoFamily, 22, FontWeight.SemiBold),
                            new SolidBrush(ColorF.FromArgb(a, 0xE8, 0xD2, 0xBE)),
                            44 + i * 5,
                            y,
                            float.PositiveInfinity,
                            TextRenderMode.AlphaAccurate);
                    }

                    c.DrawString("Rasterizer comparison and timing", ui, new SolidBrush(ColorF.FromRgb(0xD6, 0xC4, 0xB6)), 40, h - 70);
                }))
        );
    }
}
