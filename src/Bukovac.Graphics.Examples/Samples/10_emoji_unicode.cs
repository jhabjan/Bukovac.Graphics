using System;
using System.Collections.Generic;
using Bukovac.Graphics;

partial class Program
{
    private static void AddSample_10(
        List<(string Name, Action<Canvas, int, int> Render)> samples,
        FontSpec ui,
        FontSpec title,
        string uiFamily,
        string monoFamily,
        string emojiFamily)
    {
        samples.Add(
            ("10-emoji-unicode", (Action<Canvas, int, int>)((c, w, h) =>
                {
                    c.Clear(ColorF.FromRgb(0x20, 0x17, 0x2A));
                    c.FillRoundedRectangle(new SolidBrush(C(0x3A, 0x2B, 0x4E, 255)), 24, 24, w - 48, h - 48, 14);
                    c.DrawString("Emoji / Unicode sample", ui,
                        new SolidBrush(ColorF.FromRgb(0xFF, 0xF2, 0xC8)), 40, 60);
                    c.DrawString("ASCII fallback line: emojis + unicode below", ui,
                        new SolidBrush(ColorF.FromRgb(0xFF, 0xF4, 0xD1)), 40, 98);
                    c.DrawString("Emoji: 0\uFE0F\u20E3 1\uFE0F\u20E3 2\uFE0F\u20E3 3\uFE0F\u20E3 \uD83D\uDC4D \uD83D\uDC4D\uD83C\uDFFD \uD83C\uDDEC\uD83C\uDDE7 \uD83C\uDDFA\uD83C\uDDF8", new FontSpec(emojiFamily, 28),
                        new SolidBrush(ColorF.FromRgb(0xF6, 0xF6, 0xF6)), 40, 120);
                    c.DrawString("Unicode: Hrvatska \u2022 \u65E5\u672C\u8A9E \u2022 \u0420\u0443\u0441\u0441\u043A\u0438\u0439 \u2022 \u0639\u0631\u0628\u0649", ui,
                        new SolidBrush(ColorF.FromRgb(0x9E, 0xD5, 0xFF)), 40, 220);
                }))
        );
    }
}

