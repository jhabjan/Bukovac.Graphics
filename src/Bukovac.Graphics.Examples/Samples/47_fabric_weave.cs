using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_47(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("47-fabric-weave", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x11, 0x10, 0x0E));
                    c.DrawString("Fabric Weave", title, new SolidBrush(ColorF.FromRgb(0xF7, 0xEE, 0xDA)), 34, 26);
                    c.FillRoundedRectangle(new SolidBrush(C(0x1E, 0x1A, 0x15, 255)), 20, 20, w - 40, h - 40, 14);

                    int cols = 24;
                    int rows = 14;
                    float cellW = (w - 90f) / cols;
                    float cellH = (h - 140f) / rows;
                    float sx = 45f;
                    float sy = 88f;
                    for (int y = 0; y < rows; y++)
                    {
                        for (int x = 0; x < cols; x++)
                        {
                            bool over = ((x + y) % 2) == 0;
                            float px = sx + x * cellW;
                            float py = sy + y * cellH;
                            byte v = (byte)(110 + ((x * 11 + y * 7) % 90));
                            var col = over ? ColorF.FromArgb(220, (byte)(v + 60), (byte)(v + 30), v) : ColorF.FromArgb(180, v, (byte)(v - 20), (byte)(v - 35));
                            c.FillRoundedRectangle(new SolidBrush(col), px, py, cellW - 2, cellH - 2, 4);
                        }
                    }
                }))
        );
    }
}

