// SPDX-License-Identifier: GPL-3.0-or-later
//
// This file is part of Bukovac.Graphics project.
//
// Author: Josip Habjan (habjan@gmail.com, github: https://github.com/jhabjan)
// Copyright (c) 2026 Josip Habjan. All rights reserved.
//
// Bukovac.Graphics is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Bukovac.Graphics is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.

using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Collections.Generic;
using Bukovac.Graphics.Commands;

namespace Bukovac.Graphics.Rasterizers.Mac;

/// <summary>
/// CoreGraphics-based CPU rasterizer for macOS. Uses a CGBitmapContext for rendering.
/// For window mode, the rendered bitmap is set as the NSView's layer contents after each frame.
/// </summary>
[SupportedOSPlatform("macos")]
public sealed class CoreGraphicsRasterizer : IRasterizer
{
    private nint _context;
    private nint _colorSpace;
    private nint _nsView;
    private int _width;
    private int _height;
    private bool _disposed;
    private bool _isOffscreen;
    // Base transform to flip from CG's bottom-left to top-left origin
    private CGAffineTransform _flipTransform;
    private int _interpolationQuality = CG.kCGInterpolationHigh;
    private bool _shouldAntialias = true;
    private double _pixelOffsetX;
    private double _pixelOffsetY;

    public void Initialize(NativeWindowHandle window, int width, int height)
    {
        if (window.Kind != "NSView")
            throw new ArgumentException($"CoreGraphicsRasterizer requires NSView, got '{window.Kind}'");

        _nsView = window.Handle;
        _isOffscreen = false;
        CreateBitmapContext(width, height);
    }

    public void InitializeBitmap(int width, int height)
    {
        _isOffscreen = true;
        _nsView = 0;
        CreateBitmapContext(width, height);
    }

    public void Resize(int width, int height)
    {
        DestroyContext();
        CreateBitmapContext(width, height);
    }

    public void BeginFrame()
    {
        // Apply coordinate flip: CG is bottom-left origin, we use top-left
        CG.CGContextSaveGState(_context);
        CG.CGContextSetInterpolationQuality(_context, _interpolationQuality);
        CG.CGContextSetAllowsAntialiasing(_context, _shouldAntialias);
        CG.CGContextSetShouldAntialias(_context, _shouldAntialias);
        _flipTransform = new CGAffineTransform { a = 1, b = 0, c = 0, d = -1, tx = 0, ty = _height };
        CG.CGContextConcatCTM(_context, _flipTransform);
        if (_pixelOffsetX != 0.0 || _pixelOffsetY != 0.0)
        {
            CG.CGContextConcatCTM(_context, new CGAffineTransform
            {
                a = 1,
                d = 1,
                tx = _pixelOffsetX,
                ty = _pixelOffsetY,
            });
        }
    }

    public void EndFrame(ReadOnlySpan<DrawCommand> commands)
    {
        ExecuteCommands(commands);

        // Restore the pre-flip state
        CG.CGContextRestoreGState(_context);

        // For window mode, push the bitmap to the NSView's layer
        if (!_isOffscreen && _nsView != 0)
        {
            nint cgImage = CG.CGBitmapContextCreateImage(_context);
            if (cgImage != 0)
            {
                // [view.layer setContents:cgImage]
                nint selLayer = ObjC.sel_registerName("layer");
                nint layer = ObjC.Send(_nsView, selLayer);
                nint selSetContents = ObjC.sel_registerName("setContents:");
                ObjC.Send(layer, selSetContents, cgImage);
                CG.CGImageRelease(cgImage);
            }
        }
    }

    public Vector2 MeasureString(string text, FontSpec font, float maxWidth)
    {
        if (_context == 0) return Vector2.Zero;

        nint cfFontName = CF.CFStringCreateWithCString(0, font.FamilyName, CF.kCFStringEncodingUTF8);
        nint ctFont = CT.CTFontCreateWithName(cfFontName, font.Size, 0);

        nint cfText = CF.CFStringCreateWithCString(0, text, CF.kCFStringEncodingUTF8);

        // Build attributes dict with font
        nint kFont = CT.kCTFontAttributeName;
        nint attrs = CF.CFDictionaryCreate(0, [kFont], [ctFont], 1, 0, 0);
        nint attrString = CF.CFAttributedStringCreate(0, cfText, attrs);
        nint line = CT.CTLineCreateWithAttributedString(attrString);

        double width = CT.CTLineGetTypographicBounds(line, out double ascent, out double descent, out _);
        float measuredHeight = (float)(ascent + descent);

        CF.CFRelease(line);
        CF.CFRelease(attrString);
        CF.CFRelease(attrs);
        CF.CFRelease(cfText);
        CF.CFRelease(ctFont);
        CF.CFRelease(cfFontName);

        return new Vector2((float)width, measuredHeight);
    }

    public Vector2 MeasureString(string text, FontSpec font, float maxWidth, TextFormatFlags flags)
    {
        if (_context == 0 || string.IsNullOrEmpty(text))
        {
            return Vector2.Zero;
        }

        nint cfFontName = CF.CFStringCreateWithCString(0, font.FamilyName, CF.kCFStringEncodingUTF8);
        nint ctFont = CT.CTFontCreateWithName(cfFontName, font.Size, 0);
        nint kFont = CT.kCTFontAttributeName;
        nint attrs = CF.CFDictionaryCreate(0, [kFont], [ctFont], 1, 0, 0);

        try
        {
            float lineHeight = (float)(CT.CTFontGetAscent(ctFont) + CT.CTFontGetDescent(ctFont));
            bool finiteWidth = !float.IsPositiveInfinity(maxWidth);
            bool wrap = finiteWidth && (flags & TextFormatFlags.NoWrap) == 0;
            var lines = BuildWrappedLines(text, wrap ? maxWidth : float.PositiveInfinity, t => MeasureCoreTextWidth(attrs, t));

            float maxLineWidth = 0f;
            for (int i = 0; i < lines.Count; i++)
            {
                maxLineWidth = Math.Max(maxLineWidth, (float)MeasureCoreTextWidth(attrs, lines[i]));
            }

            return new Vector2(maxLineWidth, lineHeight * lines.Count);
        }
        finally
        {
            CF.CFRelease(attrs);
            CF.CFRelease(ctFont);
            CF.CFRelease(cfFontName);
        }
    }

    public void ApplyQualitySettings(InterpolationMode interpolation, SmoothingMode smoothing,
        PixelOffsetMode pixelOffset, CompositingQuality compositing)
    {
        _interpolationQuality = interpolation switch
        {
            InterpolationMode.NearestNeighbor => CG.kCGInterpolationNone,
            InterpolationMode.Bilinear => CG.kCGInterpolationLow,
            InterpolationMode.HighQualityBilinear => CG.kCGInterpolationMedium,
            InterpolationMode.Bicubic => CG.kCGInterpolationMedium,
            InterpolationMode.HighQualityBicubic => CG.kCGInterpolationHigh,
            _ => CG.kCGInterpolationDefault,
        };

        _shouldAntialias = smoothing switch
        {
            SmoothingMode.None => false,
            _ => true,
        };

        (_pixelOffsetX, _pixelOffsetY) = pixelOffset switch
        {
            PixelOffsetMode.Half => (0.5, 0.5),
            _ => (0.0, 0.0),
        };
    }

    // --- Command execution ---

    private void ExecuteCommands(ReadOnlySpan<DrawCommand> commands)
    {
        for (int i = 0; i < commands.Length; i++)
        {
            ref readonly var cmd = ref commands[i];
            switch (cmd.Kind)
            {
                case DrawCommandKind.Clear:
                    ExecuteClear(cmd.Color);
                    break;
                case DrawCommandKind.Save:
                    CG.CGContextSaveGState(_context);
                    break;
                case DrawCommandKind.Restore:
                    CG.CGContextRestoreGState(_context);
                    break;
                case DrawCommandKind.SetClip:
                    CG.CGContextClipToRect(_context, CGRect.FromRectF(cmd.Rect));
                    break;
                case DrawCommandKind.ResetClip:
                    // CG has no reset_clip. Pop and re-push state to clear clip.
                    CG.CGContextRestoreGState(_context);
                    CG.CGContextSaveGState(_context);
                    break;
                case DrawCommandKind.SetTransform:
                    ExecuteSetTransform(in cmd);
                    break;
                case DrawCommandKind.ResetTransform:
                    // Reset to the flip transform (our baseline)
                    CG.CGContextRestoreGState(_context);
                    CG.CGContextSaveGState(_context);
                    CG.CGContextConcatCTM(_context, _flipTransform);
                    if (_pixelOffsetX != 0.0 || _pixelOffsetY != 0.0)
                    {
                        CG.CGContextConcatCTM(_context, new CGAffineTransform
                        {
                            a = 1,
                            d = 1,
                            tx = _pixelOffsetX,
                            ty = _pixelOffsetY,
                        });
                    }
                    break;
                case DrawCommandKind.DrawLine:
                    ExecuteDrawLine(in cmd);
                    break;
                case DrawCommandKind.DrawRectangle:
                    ExecuteDrawRectangle(in cmd);
                    break;
                case DrawCommandKind.FillRectangle:
                    ExecuteFillRectangle(in cmd);
                    break;
                case DrawCommandKind.DrawEllipse:
                    ExecuteDrawEllipse(in cmd);
                    break;
                case DrawCommandKind.FillEllipse:
                    ExecuteFillEllipse(in cmd);
                    break;
                case DrawCommandKind.DrawString:
                    ExecuteDrawString(in cmd);
                    break;
                case DrawCommandKind.DrawImage:
                    ExecuteDrawImage(in cmd);
                    break;
                case DrawCommandKind.DrawRoundedRectangle:
                    ExecuteDrawRoundedRectangle(in cmd);
                    break;
                case DrawCommandKind.FillRoundedRectangle:
                    ExecuteFillRoundedRectangle(in cmd);
                    break;
            }
        }
    }

    private void ExecuteClear(ColorF color)
    {
        CG.CGContextSetRGBFillColor(_context, color.R, color.G, color.B, color.A);
        CG.CGContextFillRect(_context, new CGRect(0, 0, _width, _height));
    }

    private void ExecuteSetTransform(in DrawCommand cmd)
    {
        var t = new CGAffineTransform
        {
            a = cmd.Transform.M11,
            b = cmd.Transform.M12,
            c = cmd.Transform.M21,
            d = cmd.Transform.M22,
            tx = cmd.Transform.M31 + _pixelOffsetX,
            ty = cmd.Transform.M32 + _pixelOffsetY,
        };
        CG.CGContextConcatCTM(_context, t);
    }

    private void ExecuteDrawLine(in DrawCommand cmd)
    {
        CG.CGContextSetRGBStrokeColor(_context, cmd.Color.R, cmd.Color.G, cmd.Color.B, cmd.Color.A);
        CG.CGContextSetLineWidth(_context, cmd.StrokeWidth);
        SetCGDash(cmd.DashStyle, cmd.StrokeWidth);
        CG.CGContextBeginPath(_context);
        CG.CGContextMoveToPoint(_context, cmd.P1.X, cmd.P1.Y);
        CG.CGContextAddLineToPoint(_context, cmd.P2.X, cmd.P2.Y);
        CG.CGContextStrokePath(_context);
    }

    private void ExecuteDrawRectangle(in DrawCommand cmd)
    {
        CG.CGContextSetRGBStrokeColor(_context, cmd.Color.R, cmd.Color.G, cmd.Color.B, cmd.Color.A);
        CG.CGContextSetLineWidth(_context, cmd.StrokeWidth);
        SetCGDash(cmd.DashStyle, cmd.StrokeWidth);
        CG.CGContextStrokeRect(_context, CGRect.FromRectF(cmd.Rect));
    }

    private void ExecuteFillRectangle(in DrawCommand cmd)
    {
        CG.CGContextSetRGBFillColor(_context, cmd.Color.R, cmd.Color.G, cmd.Color.B, cmd.Color.A);
        CG.CGContextFillRect(_context, CGRect.FromRectF(cmd.Rect));
    }

    private void ExecuteDrawEllipse(in DrawCommand cmd)
    {
        CG.CGContextSetRGBStrokeColor(_context, cmd.Color.R, cmd.Color.G, cmd.Color.B, cmd.Color.A);
        CG.CGContextSetLineWidth(_context, cmd.StrokeWidth);
        SetCGDash(cmd.DashStyle, cmd.StrokeWidth);
        CG.CGContextStrokeEllipseInRect(_context, CGRect.FromRectF(cmd.Rect));
    }

    private void ExecuteFillEllipse(in DrawCommand cmd)
    {
        CG.CGContextSetRGBFillColor(_context, cmd.Color.R, cmd.Color.G, cmd.Color.B, cmd.Color.A);
        CG.CGContextFillEllipseInRect(_context, CGRect.FromRectF(cmd.Rect));
    }

    private void ExecuteDrawString(in DrawCommand cmd)
    {
        if (cmd.Text is null) return;

        nint cfFontName = CF.CFStringCreateWithCString(0, cmd.Font.FamilyName, CF.kCFStringEncodingUTF8);
        nint ctFont = CT.CTFontCreateWithName(cfFontName, cmd.Font.Size, 0);

        nint kFont = CT.kCTFontAttributeName;
        nint attrs = CF.CFDictionaryCreate(0, [kFont], [ctFont], 1, 0, 0);

        nint cfText = CF.CFStringCreateWithCString(0, cmd.Text, CF.kCFStringEncodingUTF8);
        nint attrString = CF.CFAttributedStringCreate(0, cfText, attrs);
        nint line = CT.CTLineCreateWithAttributedString(attrString);
        try
        {
            double textWidth = CT.CTLineGetTypographicBounds(line, out double ascent, out _, out _);
            double textX = cmd.P1.X;
            double textY = cmd.P1.Y + ascent;

            if (cmd.GlyphAdvances is { Length: > 0 } advances)
            {
                int limit = Math.Min(advances.Length, cmd.Text.Length);
                int drawWidth = 0;
                for (int i = 0; i < limit; i++)
                {
                    drawWidth += advances[i];
                }

                if (cmd.TextAlignment != TextAlignment.Near && !float.IsPositiveInfinity(cmd.MaxWidth))
                {
                    double layoutWidth = cmd.MaxWidth;
                    if (cmd.TextAlignment == TextAlignment.Center)
                        textX += (layoutWidth - drawWidth) / 2.0;
                    else
                        textX += layoutWidth - drawWidth;
                }

                int index = 0;
                while (index < cmd.Text.Length)
                {
                    int glyphLen = 1;
                    if (char.IsHighSurrogate(cmd.Text[index]) &&
                        index + 1 < cmd.Text.Length &&
                        char.IsLowSurrogate(cmd.Text[index + 1]))
                    {
                        glyphLen = 2;
                    }

                    int glyphAdvance = 0;
                    int end = Math.Min(index + glyphLen, limit);
                    for (int i = index; i < end; i++)
                    {
                        glyphAdvance += advances[i];
                    }

                    string glyph = cmd.Text.Substring(index, glyphLen);
                    DrawCoreTextGlyphRun(_context, attrs, glyph, textX, textY, cmd.Color);
                    textX += glyphAdvance;
                    index += glyphLen;
                }
            }
            else if (cmd.GlyphUniformAdvance > 0)
            {
                int drawWidth = cmd.GlyphUniformAdvance * cmd.Text.Length;
                if (cmd.TextAlignment != TextAlignment.Near && !float.IsPositiveInfinity(cmd.MaxWidth))
                {
                    double layoutWidth = cmd.MaxWidth;
                    if (cmd.TextAlignment == TextAlignment.Center)
                        textX += (layoutWidth - drawWidth) / 2.0;
                    else
                        textX += layoutWidth - drawWidth;
                }

                int index = 0;
                while (index < cmd.Text.Length)
                {
                    int glyphLen = 1;
                    if (char.IsHighSurrogate(cmd.Text[index]) &&
                        index + 1 < cmd.Text.Length &&
                        char.IsLowSurrogate(cmd.Text[index + 1]))
                    {
                        glyphLen = 2;
                    }

                    string glyph = cmd.Text.Substring(index, glyphLen);
                    DrawCoreTextGlyphRun(_context, attrs, glyph, textX, textY, cmd.Color);
                    textX += cmd.GlyphUniformAdvance * glyphLen;
                    index += glyphLen;
                }
            }
            else
            {
                bool finiteWidth = !float.IsPositiveInfinity(cmd.MaxWidth);
                bool wrap = finiteWidth && (cmd.TextFlags & TextFormatFlags.NoWrap) == 0;
                if (cmd.Text.IndexOfAny(['\r', '\n']) >= 0 || wrap)
                {
                    double lineHeight = CT.CTFontGetAscent(ctFont) + CT.CTFontGetDescent(ctFont);
                    var lines = BuildWrappedLines(cmd.Text, wrap ? cmd.MaxWidth : float.PositiveInfinity, t => MeasureCoreTextWidth(attrs, t));
                    for (int i = 0; i < lines.Count; i++)
                    {
                        string textLine = lines[i];
                        double lineX = cmd.P1.X;
                        if (cmd.TextAlignment != TextAlignment.Near && finiteWidth)
                        {
                            double lineWidth = MeasureCoreTextWidth(attrs, textLine);
                            double layoutWidth = cmd.MaxWidth;
                            if (cmd.TextAlignment == TextAlignment.Center)
                                lineX += (layoutWidth - lineWidth) / 2.0;
                            else
                                lineX += layoutWidth - lineWidth;
                        }

                        DrawCoreTextGlyphRun(_context, attrs, textLine, lineX, textY + i * lineHeight, cmd.Color);
                    }
                }
                else
                {
                    if (cmd.TextAlignment != TextAlignment.Near && !float.IsPositiveInfinity(cmd.MaxWidth))
                    {
                        double layoutWidth = cmd.MaxWidth;
                        if (cmd.TextAlignment == TextAlignment.Center)
                            textX += (layoutWidth - textWidth) / 2.0;
                        else
                            textX += layoutWidth - textWidth;
                    }

                    DrawCoreTextLine(_context, line, textX, textY, cmd.Color);
                }
            }
        }
        finally
        {
            CF.CFRelease(line);
            CF.CFRelease(attrString);
            CF.CFRelease(cfText);
            CF.CFRelease(attrs);
            CF.CFRelease(ctFont);
            CF.CFRelease(cfFontName);
        }
    }

    private static void DrawCoreTextGlyphRun(nint context, nint attrs, string glyph, double textX, double textY, ColorF color)
    {
        nint cfGlyph = CF.CFStringCreateWithCString(0, glyph, CF.kCFStringEncodingUTF8);
        nint glyphAttrString = CF.CFAttributedStringCreate(0, cfGlyph, attrs);
        nint glyphLine = CT.CTLineCreateWithAttributedString(glyphAttrString);
        try
        {
            DrawCoreTextLine(context, glyphLine, textX, textY, color);
        }
        finally
        {
            CF.CFRelease(glyphLine);
            CF.CFRelease(glyphAttrString);
            CF.CFRelease(cfGlyph);
        }
    }

    private static void DrawCoreTextLine(nint context, nint line, double textX, double textY, ColorF color)
    {
        // CoreText draws upward from baseline. Under the rasterizer y-flip, un-flip locally.
        CG.CGContextSaveGState(context);
        CG.CGContextConcatCTM(context, new CGAffineTransform
        {
            a = 1,
            b = 0,
            c = 0,
            d = -1,
            tx = 0,
            ty = textY * 2,
        });

        CG.CGContextSetRGBFillColor(context, color.R, color.G, color.B, color.A);
        CG.CGContextSetTextMatrix(context, CGAffineTransform.Identity);
        CG.CGContextConcatCTM(context, new CGAffineTransform
        {
            a = 1,
            b = 0,
            c = 0,
            d = 1,
            tx = textX,
            ty = textY,
        });
        CT.CTLineDraw(line, context);
        CG.CGContextRestoreGState(context);
    }

    private static double MeasureCoreTextWidth(nint attrs, string text)
    {
        nint cfText = CF.CFStringCreateWithCString(0, text, CF.kCFStringEncodingUTF8);
        nint attrString = CF.CFAttributedStringCreate(0, cfText, attrs);
        nint line = CT.CTLineCreateWithAttributedString(attrString);
        try
        {
            return CT.CTLineGetTypographicBounds(line, out _, out _, out _);
        }
        finally
        {
            CF.CFRelease(line);
            CF.CFRelease(attrString);
            CF.CFRelease(cfText);
        }
    }

    private static List<string> BuildWrappedLines(string text, float maxWidth, Func<string, double> measureWidth)
    {
        var lines = new List<string>();
        string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        string[] hardLines = normalized.Split('\n');
        bool finite = !float.IsPositiveInfinity(maxWidth);

        for (int h = 0; h < hardLines.Length; h++)
        {
            string hard = hardLines[h];
            if (!finite)
            {
                lines.Add(hard);
                continue;
            }

            if (hard.Length == 0)
            {
                lines.Add(string.Empty);
                continue;
            }

            string[] words = hard.Split(' ');
            string current = string.Empty;
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                string candidate = current.Length == 0 ? word : current + " " + word;
                if (current.Length == 0 || measureWidth(candidate) <= maxWidth)
                {
                    current = candidate;
                }
                else
                {
                    lines.Add(current);
                    current = word;
                }
            }
            lines.Add(current);
        }

        if (lines.Count == 0)
        {
            lines.Add(string.Empty);
        }

        return lines;
    }

    private unsafe void ExecuteDrawImage(in DrawCommand cmd)
    {
        if (!ImageManager.TryGetImageData(cmd.Image, out var imgData)) return;

        // Convert BGRA to premultiplied ARGB for CoreGraphics
        int pixelCount = imgData.Width * imgData.Height;
        byte[] argbPixels = new byte[pixelCount * 4];

        for (int p = 0; p < pixelCount; p++)
        {
            int si = p * 4;
            byte b = imgData.BgraPixels[si];
            byte g = imgData.BgraPixels[si + 1];
            byte r = imgData.BgraPixels[si + 2];
            byte a = imgData.BgraPixels[si + 3];
            // kCGImageAlphaPremultipliedFirst | kCGBitmapByteOrder32Little = BGRA premultiplied
            argbPixels[si] = (byte)(b * a / 255);
            argbPixels[si + 1] = (byte)(g * a / 255);
            argbPixels[si + 2] = (byte)(r * a / 255);
            argbPixels[si + 3] = a;
        }

        fixed (byte* pPixels = argbPixels)
        {
            nuint dataSize = (nuint)(pixelCount * 4);
            nint provider = CG.CGDataProviderCreateWithData(0, (nint)pPixels, dataSize, 0);

            nint cgImage = CG.CGImageCreate(
                (nuint)imgData.Width, (nuint)imgData.Height,
                8, 32, (nuint)(imgData.Width * 4),
                _colorSpace,
                CG.kCGImageAlphaPremultipliedFirst | CG.kCGBitmapByteOrder32Little,
                provider, 0, false, CG.kCGRenderingIntentDefault);

            int srcX;
            int srcY;
            int srcW;
            int srcH;
            if (cmd.HasSrcRect)
            {
                srcX = Math.Max(0, (int)cmd.SrcRect.X);
                srcY = Math.Max(0, (int)cmd.SrcRect.Y);
                srcW = Math.Max(1, (int)cmd.SrcRect.Width);
                srcH = Math.Max(1, (int)cmd.SrcRect.Height);
            }
            else
            {
                srcX = 0;
                srcY = 0;
                srcW = imgData.Width;
                srcH = imgData.Height;
            }

            float scaleX = cmd.Rect.Width / srcW;
            float scaleY = cmd.Rect.Height / srcH;
            float drawX = cmd.Rect.X - (srcX * scaleX);
            float drawY = cmd.Rect.Y - (srcY * scaleY);
            float drawW = imgData.Width * scaleX;
            float drawH = imgData.Height * scaleY;

            CG.CGContextSaveGState(_context);
            CG.CGContextClipToRect(_context, CGRect.FromRectF(cmd.Rect));

            if (cmd.Opacity < 1f)
                CG.CGContextSetAlpha(_context, cmd.Opacity);

            // Draw remapped full image while clipped to destination to emulate src-rect sampling.
            CG.CGContextDrawImage(_context, new CGRect(drawX, drawY, drawW, drawH), cgImage);

            if (cmd.Opacity < 1f)
                CG.CGContextSetAlpha(_context, 1.0);

            CG.CGContextRestoreGState(_context);
            CG.CGImageRelease(cgImage);
            CG.CGDataProviderRelease(provider);
        }
    }

    private void ExecuteDrawRoundedRectangle(in DrawCommand cmd)
    {
        CG.CGContextSetRGBStrokeColor(_context, cmd.Color.R, cmd.Color.G, cmd.Color.B, cmd.Color.A);
        CG.CGContextSetLineWidth(_context, cmd.StrokeWidth);
        SetCGDash(cmd.DashStyle, cmd.StrokeWidth);
        BuildRoundedRectPath(cmd.Rect, cmd.CornerRadius);
        CG.CGContextStrokePath(_context);
    }

    private void ExecuteFillRoundedRectangle(in DrawCommand cmd)
    {
        CG.CGContextSetRGBFillColor(_context, cmd.Color.R, cmd.Color.G, cmd.Color.B, cmd.Color.A);
        BuildRoundedRectPath(cmd.Rect, cmd.CornerRadius);
        CG.CGContextFillPath(_context);
    }

    private void BuildRoundedRectPath(RectF rect, float radius)
    {
        double x = rect.X;
        double y = rect.Y;
        double w = rect.Width;
        double h = rect.Height;
        double r = radius;

        CG.CGContextBeginPath(_context);
        // Top-left to top-right
        CG.CGContextMoveToPoint(_context, x + r, y);
        CG.CGContextAddLineToPoint(_context, x + w - r, y);
        // Top-right arc
        CG.CGContextAddArc(_context, x + w - r, y + r, r, -Math.PI / 2, 0, 0);
        // Right side down
        CG.CGContextAddLineToPoint(_context, x + w, y + h - r);
        // Bottom-right arc
        CG.CGContextAddArc(_context, x + w - r, y + h - r, r, 0, Math.PI / 2, 0);
        // Bottom side left
        CG.CGContextAddLineToPoint(_context, x + r, y + h);
        // Bottom-left arc
        CG.CGContextAddArc(_context, x + r, y + h - r, r, Math.PI / 2, Math.PI, 0);
        // Left side up
        CG.CGContextAddLineToPoint(_context, x, y + r);
        // Top-left arc
        CG.CGContextAddArc(_context, x + r, y + r, r, Math.PI, 3 * Math.PI / 2, 0);
        CG.CGContextClosePath(_context);
    }

    public float GetFontHeight(FontSpec font)
    {
        nint cfFontName = CF.CFStringCreateWithCString(0, font.FamilyName, CF.kCFStringEncodingUTF8);
        nint ctFont = CT.CTFontCreateWithName(cfFontName, font.Size, 0);

        double ascent = CT.CTFontGetAscent(ctFont);
        double descent = CT.CTFontGetDescent(ctFont);

        CF.CFRelease(ctFont);
        CF.CFRelease(cfFontName);

        return (float)(ascent + descent);
    }

    public bool TryCopyPixelsBgra(out int width, out int height, out byte[] bgraPixels)
    {
        width = _width;
        height = _height;
        if (_context == 0 || _width <= 0 || _height <= 0)
        {
            width = 0;
            height = 0;
            bgraPixels = [];
            return false;
        }

        nint data = CG.CGBitmapContextGetData(_context);
        if (data == 0)
        {
            width = 0;
            height = 0;
            bgraPixels = [];
            return false;
        }

        int byteCount = _width * _height * 4;
        bgraPixels = new byte[byteCount];
        Marshal.Copy(data, bgraPixels, 0, byteCount);
        return true;
    }

    // --- Helpers ---

    private void CreateBitmapContext(int width, int height)
    {
        _width = width;
        _height = height;

        _colorSpace = CG.CGColorSpaceCreateDeviceRGB();
        nuint bytesPerRow = (nuint)(width * 4);

        _context = CG.CGBitmapContextCreate(
            0, (nuint)width, (nuint)height,
            8, bytesPerRow, _colorSpace,
            CG.kCGImageAlphaPremultipliedFirst | CG.kCGBitmapByteOrder32Little);
    }

    private void DestroyContext()
    {
        if (_context != 0)
        {
            CG.CGContextRelease(_context);
            _context = 0;
        }
        if (_colorSpace != 0)
        {
            CG.CGColorSpaceRelease(_colorSpace);
            _colorSpace = 0;
        }
    }

    private void SetCGDash(DashStyle style, float strokeWidth)
    {
        double sw = strokeWidth;
        switch (style)
        {
            case DashStyle.Dash:
                CG.CGContextSetLineDash(_context, 0, [sw * 4, sw * 2], 2);
                break;
            case DashStyle.Dot:
                CG.CGContextSetLineDash(_context, 0, [sw, sw * 2], 2);
                break;
            case DashStyle.DashDot:
                CG.CGContextSetLineDash(_context, 0, [sw * 4, sw * 2, sw, sw * 2], 4);
                break;
            case DashStyle.DashDotDot:
                CG.CGContextSetLineDash(_context, 0, [sw * 4, sw * 2, sw, sw * 2, sw, sw * 2], 6);
                break;
            default: // Solid
                CG.CGContextSetLineDash(_context, 0, [], 0);
                break;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DestroyContext();
    }
}
