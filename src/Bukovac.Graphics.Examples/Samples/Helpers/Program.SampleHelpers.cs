using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Xml.Linq;
using Bukovac.Graphics;

partial class Program
{
    static ColorF C(byte r, byte g, byte b, byte a) => ColorF.FromArgb(a, r, g, b);
    
    static void DrawSierpinski(Canvas c, PointF a, PointF b, PointF d, int depth, ColorF color)
    {
        if (depth <= 0)
        {
            c.FillPolygon(new SolidBrush(color), [a, b, d]);
            return;
        }
    
        PointF ab = Mid(a, b);
        PointF bd = Mid(b, d);
        PointF da = Mid(d, a);
        DrawSierpinski(c, a, ab, da, depth - 1, color);
        DrawSierpinski(c, ab, b, bd, depth - 1, color);
        DrawSierpinski(c, da, bd, d, depth - 1, color);
    }
    
    static void DrawKochEdge(Canvas c, Pen pen, PointF a, PointF b, int depth)
    {
        if (depth <= 0)
        {
            c.DrawLine(pen, a.X, a.Y, b.X, b.Y);
            return;
        }
    
        PointF p1 = Lerp(a, b, 1f / 3f);
        PointF p3 = Lerp(a, b, 2f / 3f);
        float dx = p3.X - p1.X;
        float dy = p3.Y - p1.Y;
        PointF p2 = new(
            p1.X + (dx * 0.5f) - (dy * 0.8660254f),
            p1.Y + (dy * 0.5f) + (dx * 0.8660254f));
    
        DrawKochEdge(c, pen, a, p1, depth - 1);
        DrawKochEdge(c, pen, p1, p2, depth - 1);
        DrawKochEdge(c, pen, p2, p3, depth - 1);
        DrawKochEdge(c, pen, p3, b, depth - 1);
    }
    
    static byte[] RenderMandelbrotBgra(int width, int height, double centerX, double centerY, double scale, int maxIterations)
    {
        byte[] pixels = new byte[width * height * 4];
        double aspect = (double)width / height;
        double halfW = scale * aspect;
        double halfH = scale;
    
        for (int y = 0; y < height; y++)
        {
            double cy = centerY + ((y / (double)(height - 1)) * 2.0 - 1.0) * halfH;
            for (int x = 0; x < width; x++)
            {
                double cx = centerX + ((x / (double)(width - 1)) * 2.0 - 1.0) * halfW;
                double zx = 0.0;
                double zy = 0.0;
                int i = 0;
    
                while ((zx * zx + zy * zy <= 4.0) && i < maxIterations)
                {
                    double nx = (zx * zx) - (zy * zy) + cx;
                    zy = (2.0 * zx * zy) + cy;
                    zx = nx;
                    i++;
                }
    
                int p = ((y * width) + x) * 4;
                if (i == maxIterations)
                {
                    pixels[p + 0] = 12;
                    pixels[p + 1] = 8;
                    pixels[p + 2] = 6;
                    pixels[p + 3] = 255;
                }
                else
                {
                    float t = i / (float)maxIterations;
                    byte r = (byte)(40 + (215 * MathF.Pow(t, 0.55f)));
                    byte g = (byte)(20 + (200 * MathF.Pow(t, 0.75f)));
                    byte b = (byte)(60 + (180 * (1f - t)));
                    pixels[p + 0] = b;
                    pixels[p + 1] = g;
                    pixels[p + 2] = r;
                    pixels[p + 3] = 255;
                }
            }
        }
    
        return pixels;
    }
    
    static PointF Mid(PointF a, PointF b) => new((a.X + b.X) * 0.5f, (a.Y + b.Y) * 0.5f);
    static PointF Lerp(PointF a, PointF b, float t) => new(a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t));
    
    static void DrawBranchTree(Canvas c, float x, float y, float angle, float length, int depth, float phase)
    {
        if (depth <= 0 || length < 2f)
            return;
    
        float x2 = x + MathF.Cos(angle) * length;
        float y2 = y + MathF.Sin(angle) * length;
        byte g = (byte)Math.Clamp(88 + depth * 16, 0, 255);
        byte b = (byte)Math.Clamp(60 + depth * 9, 0, 255);
        c.DrawLine(new Pen(ColorF.FromArgb(220, 120, g, b), MathF.Max(1f, depth * 0.5f)), x, y, x2, y2);
    
        float sway = 0.15f * MathF.Sin(phase + depth * 0.37f);
        DrawBranchTree(c, x2, y2, angle - 0.40f + sway, length * 0.74f, depth - 1, phase + 0.31f);
        DrawBranchTree(c, x2, y2, angle + 0.36f - sway, length * 0.72f, depth - 1, phase + 0.57f);
        if ((depth % 2) == 0)
            DrawBranchTree(c, x2, y2, angle - 0.02f, length * 0.63f, depth - 2, phase + 0.88f);
    }
    
    static byte[] RenderVoronoiBgra(int width, int height, int seedCount)
    {
        var points = new (float X, float Y, byte R, byte G, byte B)[seedCount];
        for (int i = 0; i < seedCount; i++)
        {
            float t = i * 0.6180339f;
            float x = (0.08f + Frac(t * 1.73f) * 0.84f) * width;
            float y = (0.08f + Frac(t * 2.41f + 0.17f) * 0.84f) * height;
            points[i] = (x, y, (byte)(80 + (i * 37) % 170), (byte)(60 + (i * 59) % 170), (byte)(90 + (i * 43) % 150));
        }
    
        byte[] pixels = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float best = float.MaxValue;
                float second = float.MaxValue;
                int bestIdx = 0;
                for (int i = 0; i < points.Length; i++)
                {
                    float dx = x - points[i].X;
                    float dy = y - points[i].Y;
                    float d = dx * dx + dy * dy;
                    if (d < best)
                    {
                        second = best;
                        best = d;
                        bestIdx = i;
                    }
                    else if (d < second)
                    {
                        second = d;
                    }
                }
    
                float edge = Math.Clamp((MathF.Sqrt(second) - MathF.Sqrt(best)) / 8f, 0f, 1f);
                var p = points[bestIdx];
                byte r = (byte)(p.R * edge);
                byte g = (byte)(p.G * edge);
                byte b = (byte)(p.B * edge);
                int idx = ((y * width) + x) * 4;
                pixels[idx + 0] = b;
                pixels[idx + 1] = g;
                pixels[idx + 2] = r;
                pixels[idx + 3] = 255;
            }
        }
    
        return pixels;
    }
    
    static byte[] RenderWaveFieldBgra(int width, int height)
    {
        byte[] pixels = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            float ny = (y / (float)(height - 1)) * 2f - 1f;
            for (int x = 0; x < width; x++)
            {
                float nx = (x / (float)(width - 1)) * 2f - 1f;
                float d1 = MathF.Sqrt((nx + 0.35f) * (nx + 0.35f) + (ny + 0.15f) * (ny + 0.15f));
                float d2 = MathF.Sqrt((nx - 0.32f) * (nx - 0.32f) + (ny - 0.21f) * (ny - 0.21f));
                float v = MathF.Sin(18f * d1) + MathF.Sin(22f * d2) + MathF.Sin(10f * (nx + ny));
                float t = 0.5f + 0.5f * MathF.Sin(v);
                byte r = (byte)(20 + 200 * t);
                byte g = (byte)(30 + 170 * (1f - t));
                byte b = (byte)(70 + 180 * (0.5f + 0.5f * MathF.Sin(v + 1.3f)));
                int idx = ((y * width) + x) * 4;
                pixels[idx + 0] = b;
                pixels[idx + 1] = g;
                pixels[idx + 2] = r;
                pixels[idx + 3] = 255;
            }
        }
    
        return pixels;
    }
    
    static byte[] RenderJuliaSetBgra(int width, int height, double cRe, double cIm, int maxIterations)
    {
        byte[] pixels = new byte[width * height * 4];
        double aspect = (double)width / height;
        for (int y = 0; y < height; y++)
        {
            double zy0 = ((y / (double)(height - 1)) * 2.0 - 1.0) * 1.35;
            for (int x = 0; x < width; x++)
            {
                double zx0 = ((x / (double)(width - 1)) * 2.0 - 1.0) * 1.35 * aspect;
                double zx = zx0;
                double zy = zy0;
                int i = 0;
                while ((zx * zx + zy * zy <= 4.0) && i < maxIterations)
                {
                    double nx = zx * zx - zy * zy + cRe;
                    zy = 2.0 * zx * zy + cIm;
                    zx = nx;
                    i++;
                }
    
                int idx = ((y * width) + x) * 4;
                if (i == maxIterations)
                {
                    pixels[idx + 0] = 8;
                    pixels[idx + 1] = 5;
                    pixels[idx + 2] = 4;
                }
                else
                {
                    float t = i / (float)maxIterations;
                    pixels[idx + 2] = (byte)(40 + 220 * MathF.Pow(t, 0.55f));
                    pixels[idx + 1] = (byte)(30 + 160 * MathF.Pow(1f - t, 0.75f));
                    pixels[idx + 0] = (byte)(80 + 150 * MathF.Sin(3.14f * t));
                }
                pixels[idx + 3] = 255;
            }
        }
    
        return pixels;
    }
    
    static int[] GenerateMaze(int cols, int rows, int seed)
    {
        int total = cols * rows;
        int[] walls = new int[total];
        bool[] seen = new bool[total];
        Array.Fill(walls, 15);
        var rnd = new Random(seed);
        var stack = new Stack<int>(total);
        int start = 0;
        stack.Push(start);
        seen[start] = true;
    
        while (stack.Count > 0)
        {
            int cur = stack.Peek();
            int cx = cur % cols;
            int cy = cur / cols;
            var neighbors = new List<(int Index, int Dir)>(4);
            if (cy > 0 && !seen[cur - cols]) neighbors.Add((cur - cols, 0));
            if (cx < cols - 1 && !seen[cur + 1]) neighbors.Add((cur + 1, 1));
            if (cy < rows - 1 && !seen[cur + cols]) neighbors.Add((cur + cols, 2));
            if (cx > 0 && !seen[cur - 1]) neighbors.Add((cur - 1, 3));
    
            if (neighbors.Count == 0)
            {
                stack.Pop();
                continue;
            }
    
            var pick = neighbors[rnd.Next(neighbors.Count)];
            int next = pick.Index;
            int dir = pick.Dir;
            if (dir == 0) { walls[cur] &= ~1; walls[next] &= ~4; }
            else if (dir == 1) { walls[cur] &= ~2; walls[next] &= ~8; }
            else if (dir == 2) { walls[cur] &= ~4; walls[next] &= ~1; }
            else { walls[cur] &= ~8; walls[next] &= ~2; }
    
            seen[next] = true;
            stack.Push(next);
        }
    
        return walls;
    }
    
    static float FieldHeight(float x, float y)
    {
        float v1 = MathF.Sin(x * 1.35f) * MathF.Cos(y * 1.72f);
        float v2 = 0.5f * MathF.Sin((x + y) * 2.1f + 1.4f);
        float v3 = 0.35f * MathF.Cos(MathF.Sqrt((x * x) + (y * y)) * 3.4f);
        return v1 + v2 + v3;
    }
    
    static void DrawRecursiveTiles(Canvas c, RectF rect, int depth, int seed)
    {
        if (depth <= 0 || rect.Width < 14 || rect.Height < 14)
        {
            byte r = (byte)(80 + ((seed * 37) & 0x7F));
            byte g = (byte)(70 + ((seed * 53) & 0x7F));
            byte b = (byte)(50 + ((seed * 71) & 0x7F));
            c.FillRoundedRectangle(new SolidBrush(C(r, g, b, 190)), rect, 5);
            c.DrawRoundedRectangle(new Pen(ColorF.FromArgb(210, 240, 220, 180), 1), rect, 5);
            return;
        }
    
        float splitX = rect.X + rect.Width * (0.32f + 0.36f * Frac(seed * 0.000173f));
        float splitY = rect.Y + rect.Height * (0.32f + 0.36f * Frac(seed * 0.000297f));
        DrawRecursiveTiles(c, new RectF(rect.X, rect.Y, splitX - rect.X, splitY - rect.Y), depth - 1, seed * 1664525 + 1013904223);
        DrawRecursiveTiles(c, new RectF(splitX, rect.Y, rect.Right - splitX, splitY - rect.Y), depth - 1, seed * 1103515245 + 12345);
        DrawRecursiveTiles(c, new RectF(rect.X, splitY, splitX - rect.X, rect.Bottom - splitY), depth - 1, seed * 214013 + 2531011);
        DrawRecursiveTiles(c, new RectF(splitX, splitY, rect.Right - splitX, rect.Bottom - splitY), depth - 1, seed * 134775813 + 1);
    }
    
    static float Frac(float v) => v - MathF.Floor(v);
    
    static void DrawIsoBox(Canvas c, float cx, float cy, float bx, float bz, float w, float d, float h, ColorF baseColor)
    {
        // Simple isometric projection (x,z) -> screen; y uses height.
        PointF P(float x, float y, float z) => new(cx + (x - z), cy + (x + z) * 0.5f - y);
        var p000 = P(bx, 0, bz);
        var p100 = P(bx + w, 0, bz);
        var p010 = P(bx, h, bz);
        var p110 = P(bx + w, h, bz);
        var p001 = P(bx, 0, bz + d);
        var p101 = P(bx + w, 0, bz + d);
        var p011 = P(bx, h, bz + d);
        var p111 = P(bx + w, h, bz + d);
    
        ColorF top = ColorF.FromRgb(
            (byte)Math.Min(255, (int)(baseColor.R * 255f) + 26),
            (byte)Math.Min(255, (int)(baseColor.G * 255f) + 26),
            (byte)Math.Min(255, (int)(baseColor.B * 255f) + 26));
        ColorF left = ColorF.FromRgb(
            (byte)Math.Max(0, (int)(baseColor.R * 255f) - 20),
            (byte)Math.Max(0, (int)(baseColor.G * 255f) - 22),
            (byte)Math.Max(0, (int)(baseColor.B * 255f) - 24));
        ColorF right = ColorF.FromRgb(
            (byte)Math.Max(0, (int)(baseColor.R * 255f) - 36),
            (byte)Math.Max(0, (int)(baseColor.G * 255f) - 40),
            (byte)Math.Max(0, (int)(baseColor.B * 255f) - 46));
    
        c.FillPolygon(new SolidBrush(left), [p000, p001, p011, p010]);
        c.FillPolygon(new SolidBrush(right), [p001, p101, p111, p011]);
        c.FillPolygon(new SolidBrush(top), [p010, p011, p111, p110]);
        c.DrawPolygon(new Pen(ColorF.FromArgb(120, 20, 24, 34), 1), [p000, p001, p011, p010]);
        c.DrawPolygon(new Pen(ColorF.FromArgb(120, 20, 24, 34), 1), [p001, p101, p111, p011]);
        c.DrawPolygon(new Pen(ColorF.FromArgb(150, 28, 32, 44), 1), [p010, p011, p111, p110]);
    }
    
    static void DrawOrnateCurl(Canvas c, float x, float y, float s, int dir, ColorF color)
    {
        var pen = new Pen(color, 2);
        c.DrawBezier(pen, x, y, x + dir * s * 1.8f, y - s * 0.9f, x + dir * s * 1.6f, y + s * 0.9f, x + dir * s * 0.3f, y + s * 1.2f);
        c.DrawBezier(pen, x + dir * s * 0.2f, y - s * 0.4f, x + dir * s * 0.9f, y - s * 1.2f, x + dir * s * 1.3f, y + s * 0.2f, x + dir * s * 0.5f, y + s * 0.4f);
        c.FillEllipse(new SolidBrush(C(0xCC, 0xB5, 0x8F, 255)), x + dir * s * 1.05f - 2, y - s * 0.2f - 2, 4, 4);
    }
    
    static void DrawRamMotif(Canvas c, RectF r)
    {
        // stylized engraved ram silhouette
        c.FillEllipse(new SolidBrush(C(0x8D, 0x7A, 0x5A, 255)), r.X + r.Width * 0.2f, r.Y + r.Height * 0.35f, r.Width * 0.55f, r.Height * 0.35f);
        c.FillEllipse(new SolidBrush(C(0x8D, 0x7A, 0x5A, 255)), r.X + r.Width * 0.1f, r.Y + r.Height * 0.30f, r.Width * 0.18f, r.Height * 0.18f);
        c.DrawEllipse(new Pen(ColorF.FromRgb(0x2A, 0x25, 0x1F), 4), r.X + r.Width * 0.02f, r.Y + r.Height * 0.16f, r.Width * 0.18f, r.Height * 0.22f);
        c.DrawLine(new Pen(ColorF.FromRgb(0x2A, 0x25, 0x1F), 4), r.X + r.Width * 0.30f, r.Y + r.Height * 0.70f, r.X + r.Width * 0.28f, r.Bottom - 8);
        c.DrawLine(new Pen(ColorF.FromRgb(0x2A, 0x25, 0x1F), 4), r.X + r.Width * 0.52f, r.Y + r.Height * 0.70f, r.X + r.Width * 0.50f, r.Bottom - 8);
        c.DrawLine(new Pen(ColorF.FromRgb(0x2A, 0x25, 0x1F), 4), r.X + r.Width * 0.70f, r.Y + r.Height * 0.70f, r.X + r.Width * 0.68f, r.Bottom - 8);
        c.DrawLine(new Pen(ColorF.FromRgb(0x2A, 0x25, 0x1F), 4), r.X + r.Width * 0.82f, r.Y + r.Height * 0.67f, r.X + r.Width * 0.90f, r.Y + r.Height * 0.62f);
    }
    
    static byte[] RenderOrbLandscapeBgra(int width, int height)
    {
        byte[] pixels = new byte[width * height * 4];
        float cx = (width - 1) * 0.5f;
        float cy = (height - 1) * 0.5f;
        float rr = MathF.Min(width, height) * 0.5f - 2;
    
        for (int y = 0; y < height; y++)
        {
            float ny = y / (float)Math.Max(1, height - 1);
            for (int x = 0; x < width; x++)
            {
                int p = ((y * width) + x) * 4;
                float dx = x - cx;
                float dy = y - cy;
                float d = MathF.Sqrt(dx * dx + dy * dy);
                if (d > rr)
                {
                    pixels[p + 3] = 0;
                    continue;
                }
    
                // sky -> horizon -> land -> water
                byte r, g, b;
                if (ny < 0.45f)
                {
                    float t = ny / 0.45f;
                    r = (byte)(255 - 80 * t);
                    g = (byte)(215 - 40 * t);
                    b = (byte)(150 + 60 * t);
                }
                else if (ny < 0.62f)
                {
                    float t = (ny - 0.45f) / 0.17f;
                    r = (byte)(190 - 60 * t);
                    g = (byte)(160 - 30 * t);
                    b = (byte)(90 - 30 * t);
                }
                else
                {
                    float t = (ny - 0.62f) / 0.38f;
                    r = (byte)(40 + 50 * t);
                    g = (byte)(95 + 70 * t);
                    b = (byte)(115 + 80 * t);
                }
    
                // mountain silhouette
                float mx = (x / (float)Math.Max(1, width - 1));
                float peak = 0.36f + MathF.Abs(mx - 0.63f) * 1.8f + 0.02f * MathF.Sin(mx * 26f);
                if (ny > peak && ny < 0.62f)
                {
                    r = (byte)(120 + 35 * (1f - ny));
                    g = (byte)(92 + 24 * (1f - ny));
                    b = (byte)(76 + 20 * (1f - ny));
                }
    
                // circular highlight + vignette
                float edge = Math.Clamp(1f - d / rr, 0f, 1f);
                float glow = MathF.Exp(-((dx + rr * 0.22f) * (dx + rr * 0.22f) + (dy + rr * 0.32f) * (dy + rr * 0.32f)) / (rr * rr * 0.35f));
                r = (byte)Math.Clamp((int)(r * (0.72f + edge * 0.32f) + 45 * glow), 0, 255);
                g = (byte)Math.Clamp((int)(g * (0.72f + edge * 0.32f) + 35 * glow), 0, 255);
                b = (byte)Math.Clamp((int)(b * (0.72f + edge * 0.32f) + 12 * glow), 0, 255);
    
                pixels[p + 0] = b;
                pixels[p + 1] = g;
                pixels[p + 2] = r;
                pixels[p + 3] = 255;
            }
        }
    
        return pixels;
    }
    
    static byte[] RenderGradientSunriseBgra(int width, int height)
    {
        byte[] pixels = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            float t = y / (float)Math.Max(1, height - 1);
            byte r = (byte)(18 + (220 * MathF.Pow(1f - t, 0.45f)));
            byte g = (byte)(24 + (180 * (1f - t)) + (35 * t));
            byte b = (byte)(55 + (130 * t * t));
            for (int x = 0; x < width; x++)
            {
                float nx = x / (float)Math.Max(1, width - 1);
                float haze = 0.12f * MathF.Sin(nx * 12f + y * 0.03f);
                int idx = ((y * width) + x) * 4;
                pixels[idx + 2] = (byte)Math.Clamp((int)(r + 38 * haze), 0, 255);
                pixels[idx + 1] = (byte)Math.Clamp((int)(g + 22 * haze), 0, 255);
                pixels[idx + 0] = (byte)Math.Clamp((int)(b + 18 * haze), 0, 255);
                pixels[idx + 3] = 255;
            }
        }
    
        float cx = width * 0.5f;
        float cy = height * 0.62f;
        float rad = MathF.Min(width, height) * 0.22f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float d = MathF.Sqrt(dx * dx + dy * dy);
                if (d > rad * 1.6f) continue;
                float glow = Math.Clamp(1f - d / (rad * 1.6f), 0f, 1f);
                int idx = ((y * width) + x) * 4;
                int rr = pixels[idx + 2] + (int)(120 * glow);
                int gg = pixels[idx + 1] + (int)(90 * glow);
                int bb = pixels[idx + 0] + (int)(30 * glow);
                pixels[idx + 2] = (byte)Math.Min(255, rr);
                pixels[idx + 1] = (byte)Math.Min(255, gg);
                pixels[idx + 0] = (byte)Math.Min(255, bb);
            }
        }
    
        return pixels;
    }
    
    static byte[] RenderRealisticTerrainBgra(int width, int height)
    {
        byte[] pixels = new byte[width * height * 4];
        var heights = new float[width * height];
    
        for (int y = 0; y < height; y++)
        {
            float ny = y / (float)(height - 1);
            for (int x = 0; x < width; x++)
            {
                float nx = x / (float)(width - 1);
                float px = (nx * 2f - 1f) * 3.8f;
                float py = (ny * 2f - 1f) * 2.8f;
    
                float e = 0f;
                float amp = 1f;
                float freq = 1f;
                float norm = 0f;
                for (int o = 0; o < 6; o++)
                {
                    float n = MathF.Sin(px * freq * 1.23f + 0.7f) * MathF.Cos(py * freq * 1.11f - 0.35f);
                    n += 0.5f * MathF.Sin((px + py) * freq * 1.7f);
                    e += n * amp;
                    norm += amp;
                    amp *= 0.52f;
                    freq *= 1.95f;
                }
                e /= norm;
                e = 0.5f + 0.5f * e;
                e = MathF.Pow(e, 1.35f);
                heights[(y * width) + x] = e;
            }
        }
    
        float lx = -0.58f;
        float ly = -0.45f;
        float lz = 0.68f;
        float lInv = 1f / MathF.Sqrt(lx * lx + ly * ly + lz * lz);
        lx *= lInv;
        ly *= lInv;
        lz *= lInv;
    
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = (y * width) + x;
                float h = heights[idx];
                int xL = Math.Max(0, x - 1);
                int xR = Math.Min(width - 1, x + 1);
                int yU = Math.Max(0, y - 1);
                int yD = Math.Min(height - 1, y + 1);
                float dhx = heights[(y * width) + xR] - heights[(y * width) + xL];
                float dhy = heights[(yD * width) + x] - heights[(yU * width) + x];
    
                float nx = -dhx * 2.2f;
                float ny = -dhy * 2.2f;
                float nz = 1f;
                float invN = 1f / MathF.Sqrt(nx * nx + ny * ny + nz * nz);
                nx *= invN;
                ny *= invN;
                nz *= invN;
    
                float diffuse = MathF.Max(0f, nx * lx + ny * ly + nz * lz);
                float ambient = 0.34f;
                float light = ambient + diffuse * 0.95f;
    
                byte br;
                byte bg;
                byte bb;
                if (h < 0.30f)
                {
                    br = 42; bg = 80; bb = 126; // water
                }
                else if (h < 0.42f)
                {
                    br = 208; bg = 192; bb = 142; // beach
                }
                else if (h < 0.68f)
                {
                    br = 78; bg = 126; bb = 72; // grass
                }
                else if (h < 0.84f)
                {
                    br = 118; bg = 108; bb = 98; // rock
                }
                else
                {
                    br = 232; bg = 236; bb = 242; // snow
                }
    
                float depth = y / (float)(height - 1);
                float fog = MathF.Pow(depth, 1.7f) * 0.55f;
                float fr = 168f;
                float fg = 190f;
                float fb = 214f;
    
                float rLit = Math.Clamp(br * light, 0f, 255f);
                float gLit = Math.Clamp(bg * light, 0f, 255f);
                float bLit = Math.Clamp(bb * light, 0f, 255f);
    
                float rOut = (rLit * (1f - fog)) + (fr * fog);
                float gOut = (gLit * (1f - fog)) + (fg * fog);
                float bOut = (bLit * (1f - fog)) + (fb * fog);
    
                int p = idx * 4;
                pixels[p + 0] = (byte)Math.Clamp((int)bOut, 0, 255);
                pixels[p + 1] = (byte)Math.Clamp((int)gOut, 0, 255);
                pixels[p + 2] = (byte)Math.Clamp((int)rOut, 0, 255);
                pixels[p + 3] = 255;
            }
        }
    
        return pixels;
    }
    
    static byte[] RenderSpaceStarfieldBgra(int width, int height)
    {
        byte[] pixels = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            float ny = y / (float)(height - 1);
            for (int x = 0; x < width; x++)
            {
                float nx = x / (float)(width - 1);
                float vignette = 1f - 0.82f * MathF.Pow(MathF.Abs(nx * 2f - 1f), 2.0f) - 0.65f * MathF.Pow(MathF.Abs(ny * 2f - 1f), 2.0f);
                vignette = Math.Clamp(vignette, 0.08f, 1f);
                float n = 0.5f + 0.5f * MathF.Sin((x * 0.087f) + (y * 0.151f) + 1.7f);
                byte b = (byte)(14 + 36 * vignette + 24 * n);
                byte g = (byte)(8 + 20 * vignette + 10 * n);
                byte r = (byte)(4 + 16 * vignette + 6 * n);
                int idx = ((y * width) + x) * 4;
                pixels[idx + 0] = b;
                pixels[idx + 1] = g;
                pixels[idx + 2] = r;
                pixels[idx + 3] = 255;
            }
        }
    
        int starCount = Math.Max(400, (width * height) / 2200);
        for (int i = 0; i < starCount; i++)
        {
            float fx = Frac(i * 0.6180339f + 0.1337f);
            float fy = Frac(i * 0.41421356f + 0.73f);
            int sx = (int)(fx * (width - 1));
            int sy = (int)(fy * (height - 1));
            byte s = (byte)(150 + (i * 37 % 105));
            PlotStar(pixels, width, height, sx, sy, s);
        }
    
        return pixels;
    }
    
    static byte[] RenderNebulaBgra(int width, int height)
    {
        byte[] pixels = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            float ny = (y / (float)(height - 1)) * 2f - 1f;
            for (int x = 0; x < width; x++)
            {
                float nx = (x / (float)(width - 1)) * 2f - 1f;
                float v = 0f;
                float amp = 1f;
                float freq = 0.9f;
                for (int o = 0; o < 5; o++)
                {
                    float n = MathF.Sin((nx + 0.3f) * freq * 3.1f + 0.6f) * MathF.Cos((ny - 0.2f) * freq * 2.7f);
                    n += 0.6f * MathF.Sin((nx - ny) * freq * 4.6f + 1.1f);
                    v += n * amp;
                    amp *= 0.55f;
                    freq *= 1.92f;
                }
                float d = MathF.Sqrt(nx * nx + ny * ny);
                float mask = Math.Clamp(1.2f - d, 0f, 1f);
                float t = Math.Clamp(0.5f + 0.33f * v, 0f, 1f) * mask;
                byte r = (byte)(25 + 210 * t);
                byte g = (byte)(10 + 150 * MathF.Pow(t, 0.8f));
                byte b = (byte)(40 + 220 * MathF.Pow(t, 0.55f));
                int idx = ((y * width) + x) * 4;
                pixels[idx + 0] = b;
                pixels[idx + 1] = g;
                pixels[idx + 2] = r;
                pixels[idx + 3] = 255;
            }
        }
    
        for (int i = 0; i < Math.Max(220, (width * height) / 5000); i++)
        {
            int sx = (int)(Frac(i * 0.754877f + 0.19f) * (width - 1));
            int sy = (int)(Frac(i * 0.5698403f + 0.51f) * (height - 1));
            PlotStar(pixels, width, height, sx, sy, (byte)(120 + (i * 31 % 120)));
        }
    
        return pixels;
    }
    
    static byte[] RenderCatFaceBgra(int width, int height, int variant)
    {
        byte[] pixels = new byte[width * height * 4];
        byte furR = (byte)(190 + (variant * 8));
        byte furG = (byte)(150 + (variant * 4));
        byte furB = (byte)(120 + (variant * 3));
        byte bgR = (byte)(34 + variant * 6);
        byte bgG = (byte)(42 + variant * 7);
        byte bgB = (byte)(48 + variant * 8);
    
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = ((y * width) + x) * 4;
                pixels[idx + 0] = bgB;
                pixels[idx + 1] = bgG;
                pixels[idx + 2] = bgR;
                pixels[idx + 3] = 255;
            }
        }
    
        float cx = width * 0.5f;
        float cy = height * 0.58f;
        float rx = width * 0.33f;
        float ry = height * 0.34f;
        PaintEllipse(pixels, width, height, cx, cy, rx, ry, furR, furG, furB);
        PaintTriangle(pixels, width, height, cx - rx * 0.74f, cy - ry * 0.72f, cx - rx * 0.26f, cy - ry * 1.36f, cx - rx * 0.06f, cy - ry * 0.52f, furR, furG, furB);
        PaintTriangle(pixels, width, height, cx + rx * 0.74f, cy - ry * 0.72f, cx + rx * 0.26f, cy - ry * 1.36f, cx + rx * 0.06f, cy - ry * 0.52f, furR, furG, furB);
        PaintEllipse(pixels, width, height, cx - rx * 0.36f, cy - ry * 0.10f, rx * 0.14f, ry * 0.18f, 35, 50, 22);
        PaintEllipse(pixels, width, height, cx + rx * 0.36f, cy - ry * 0.10f, rx * 0.14f, ry * 0.18f, 35, 50, 22);
        PaintEllipse(pixels, width, height, cx - rx * 0.36f, cy - ry * 0.12f, rx * 0.05f, ry * 0.08f, 230, 245, 220);
        PaintEllipse(pixels, width, height, cx + rx * 0.36f, cy - ry * 0.12f, rx * 0.05f, ry * 0.08f, 230, 245, 220);
        PaintTriangle(pixels, width, height, cx, cy + ry * 0.00f, cx - rx * 0.07f, cy + ry * 0.12f, cx + rx * 0.07f, cy + ry * 0.12f, 230, 140, 140);
        PaintLine(pixels, width, height, cx - rx * 0.18f, cy + ry * 0.10f, cx - rx * 0.58f, cy + ry * 0.14f, 230, 230, 230);
        PaintLine(pixels, width, height, cx - rx * 0.18f, cy + ry * 0.16f, cx - rx * 0.62f, cy + ry * 0.28f, 230, 230, 230);
        PaintLine(pixels, width, height, cx + rx * 0.18f, cy + ry * 0.10f, cx + rx * 0.58f, cy + ry * 0.14f, 230, 230, 230);
        PaintLine(pixels, width, height, cx + rx * 0.18f, cy + ry * 0.16f, cx + rx * 0.62f, cy + ry * 0.28f, 230, 230, 230);
        return pixels;
    }
    
    static void PlotStar(byte[] pixels, int width, int height, int x, int y, byte v)
    {
        if ((uint)x >= (uint)width || (uint)y >= (uint)height) return;
        for (int oy = -1; oy <= 1; oy++)
        {
            int py = y + oy;
            if ((uint)py >= (uint)height) continue;
            for (int ox = -1; ox <= 1; ox++)
            {
                int px = x + ox;
                if ((uint)px >= (uint)width) continue;
                float w = (ox == 0 && oy == 0) ? 1f : 0.35f;
                int idx = ((py * width) + px) * 4;
                int b = pixels[idx + 0] + (int)(v * w);
                int g = pixels[idx + 1] + (int)(v * w);
                int r = pixels[idx + 2] + (int)(v * w);
                pixels[idx + 0] = (byte)Math.Min(255, b);
                pixels[idx + 1] = (byte)Math.Min(255, g);
                pixels[idx + 2] = (byte)Math.Min(255, r);
            }
        }
    }
    
    static void PaintEllipse(byte[] pixels, int width, int height, float cx, float cy, float rx, float ry, byte r, byte g, byte b)
    {
        int x0 = Math.Max(0, (int)(cx - rx - 1));
        int x1 = Math.Min(width - 1, (int)(cx + rx + 1));
        int y0 = Math.Max(0, (int)(cy - ry - 1));
        int y1 = Math.Min(height - 1, (int)(cy + ry + 1));
        for (int y = y0; y <= y1; y++)
        {
            float ny = (y - cy) / ry;
            for (int x = x0; x <= x1; x++)
            {
                float nx = (x - cx) / rx;
                if ((nx * nx + ny * ny) <= 1f)
                {
                    int idx = ((y * width) + x) * 4;
                    pixels[idx + 0] = b;
                    pixels[idx + 1] = g;
                    pixels[idx + 2] = r;
                    pixels[idx + 3] = 255;
                }
            }
        }
    }
    
    static void PaintTriangle(byte[] pixels, int width, int height, float ax, float ay, float bx, float by, float cx, float cy, byte r, byte g, byte b)
    {
        int x0 = Math.Max(0, (int)MathF.Floor(MathF.Min(ax, MathF.Min(bx, cx))));
        int x1 = Math.Min(width - 1, (int)MathF.Ceiling(MathF.Max(ax, MathF.Max(bx, cx))));
        int y0 = Math.Max(0, (int)MathF.Floor(MathF.Min(ay, MathF.Min(by, cy))));
        int y1 = Math.Min(height - 1, (int)MathF.Ceiling(MathF.Max(ay, MathF.Max(by, cy))));
        float area = Edge(ax, ay, bx, by, cx, cy);
        if (MathF.Abs(area) < 0.0001f) return;
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                float w0 = Edge(bx, by, cx, cy, x, y);
                float w1 = Edge(cx, cy, ax, ay, x, y);
                float w2 = Edge(ax, ay, bx, by, x, y);
                bool inside = (w0 >= 0 && w1 >= 0 && w2 >= 0) || (w0 <= 0 && w1 <= 0 && w2 <= 0);
                if (inside)
                {
                    int idx = ((y * width) + x) * 4;
                    pixels[idx + 0] = b;
                    pixels[idx + 1] = g;
                    pixels[idx + 2] = r;
                    pixels[idx + 3] = 255;
                }
            }
        }
    }
    
    static void PaintLine(byte[] pixels, int width, int height, float x0, float y0, float x1, float y1, byte r, byte g, byte b)
    {
        int steps = Math.Max(1, (int)MathF.Ceiling(MathF.Max(MathF.Abs(x1 - x0), MathF.Abs(y1 - y0))));
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            int x = (int)(x0 + (x1 - x0) * t);
            int y = (int)(y0 + (y1 - y0) * t);
            if ((uint)x >= (uint)width || (uint)y >= (uint)height) continue;
            int idx = ((y * width) + x) * 4;
            pixels[idx + 0] = b;
            pixels[idx + 1] = g;
            pixels[idx + 2] = r;
            pixels[idx + 3] = 255;
        }
    }
    
    static float Edge(float ax, float ay, float bx, float by, float px, float py) => (px - ax) * (by - ay) - (py - ay) * (bx - ax);
    
    static bool DrawSvgToCanvas(Canvas c, string svgContent, RectF targetRect)
    {
        if (string.IsNullOrWhiteSpace(svgContent))
            return false;
    
        try
        {
            var doc = XDocument.Parse(svgContent, LoadOptions.None);
            var root = doc.Root;
            if (root is null || root.Name.LocalName != "svg")
                return false;
    
            var polys = new List<(List<PointF> Points, ColorF Fill)>(2048);
            CollectSvgElementRecursive(root, Matrix3x2.Identity, null, polys);
            if (polys.Count == 0)
                return false;
    
            if (!TryGetPolylineBounds(polys, out RectF bounds) || bounds.Width <= 0 || bounds.Height <= 0)
                return false;
    
            float sx = targetRect.Width / bounds.Width;
            float sy = targetRect.Height / bounds.Height;
            float scale = MathF.Min(sx, sy);
            float ox = targetRect.X + ((targetRect.Width - (bounds.Width * scale)) * 0.5f) - (bounds.X * scale);
            float oy = targetRect.Y + ((targetRect.Height - (bounds.Height * scale)) * 0.5f) - (bounds.Y * scale);
            var fit = Matrix3x2.CreateScale(scale, scale) * Matrix3x2.CreateTranslation(ox, oy);
    
            c.Save();
            c.SetTransform(fit);
            for (int i = 0; i < polys.Count; i++)
            {
                var p = polys[i];
                if (p.Points.Count >= 3)
                    c.FillPolygon(new SolidBrush(p.Fill), p.Points.ToArray());
            }
            c.Restore();
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    static void CollectSvgElementRecursive(XElement el, Matrix3x2 parentTransform, ColorF? inheritedFill, List<(List<PointF> Points, ColorF Fill)> outPolys)
    {
        Matrix3x2 local = parentTransform;
        string? transformAttr = (string?)el.Attribute("transform");
        if (!string.IsNullOrWhiteSpace(transformAttr))
            local = parentTransform * ParseSvgTransform(transformAttr);
    
        ColorF? fill = inheritedFill;
        string? fillAttr = (string?)el.Attribute("fill");
        if (!string.IsNullOrWhiteSpace(fillAttr))
            fill = ParseSvgColor(fillAttr);
    
        if (el.Name.LocalName == "path")
        {
            string? d = (string?)el.Attribute("d");
            if (!string.IsNullOrWhiteSpace(d) && fill is ColorF fc)
                CollectSvgPathData(d, local, fc, outPolys);
        }
    
        foreach (var child in el.Elements())
            CollectSvgElementRecursive(child, local, fill, outPolys);
    }
    
    static void CollectSvgPathData(string d, Matrix3x2 transform, ColorF fill, List<(List<PointF> Points, ColorF Fill)> outPolys)
    {
        var subpaths = ParseSvgPathToPolygons(d, transform);
        for (int i = 0; i < subpaths.Count; i++)
        {
            var poly = subpaths[i];
            if (poly.Count >= 3)
                outPolys.Add((poly, fill));
        }
    }
    
    static bool TryGetPolylineBounds(List<(List<PointF> Points, ColorF Fill)> polys, out RectF bounds)
    {
        bool hasPoint = false;
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;
        for (int i = 0; i < polys.Count; i++)
        {
            var pts = polys[i].Points;
            for (int j = 0; j < pts.Count; j++)
            {
                var p = pts[j];
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
                hasPoint = true;
            }
        }
    
        if (!hasPoint)
        {
            bounds = new RectF(0, 0, 0, 0);
            return false;
        }
    
        bounds = new RectF(minX, minY, maxX - minX, maxY - minY);
        return true;
    }
    
    static List<List<PointF>> ParseSvgPathToPolygons(string d, Matrix3x2 transform)
    {
        var result = new List<List<PointF>>();
        int i = 0;
        char cmd = '\0';
        PointF cur = new(0, 0);
        PointF subStart = new(0, 0);
        PointF lastC2 = new(0, 0);
        PointF lastQ = new(0, 0);
        bool hasLastC2 = false;
        bool hasLastQ = false;
        List<PointF>? poly = null;
    
        while (true)
        {
            SkipSvgWs(d, ref i);
            if (i >= d.Length) break;
            if (IsSvgCmd(d[i]))
                cmd = d[i++];
            else if (cmd == '\0')
                break;
    
            bool rel = char.IsLower(cmd);
            char uc = char.ToUpperInvariant(cmd);
            if (uc == 'M')
            {
                float x = ReadSvgFloat(d, ref i);
                float y = ReadSvgFloat(d, ref i);
                cur = rel ? new(cur.X + x, cur.Y + y) : new(x, y);
                subStart = cur;
                poly = new List<PointF>(256) { TransformPoint(cur, transform) };
                result.Add(poly);
                hasLastC2 = false;
                hasLastQ = false;
    
                while (TryReadSvgFloat(d, ref i, out x))
                {
                    y = ReadSvgFloat(d, ref i);
                    cur = rel ? new(cur.X + x, cur.Y + y) : new(x, y);
                    poly.Add(TransformPoint(cur, transform));
                }
            }
            else if (uc == 'L' && poly is not null)
            {
                while (TryReadSvgFloat(d, ref i, out float x))
                {
                    float y = ReadSvgFloat(d, ref i);
                    cur = rel ? new(cur.X + x, cur.Y + y) : new(x, y);
                    poly.Add(TransformPoint(cur, transform));
                }
                hasLastC2 = false;
                hasLastQ = false;
            }
            else if (uc == 'H' && poly is not null)
            {
                while (TryReadSvgFloat(d, ref i, out float x))
                {
                    cur = rel ? new(cur.X + x, cur.Y) : new(x, cur.Y);
                    poly.Add(TransformPoint(cur, transform));
                }
                hasLastC2 = false;
                hasLastQ = false;
            }
            else if (uc == 'V' && poly is not null)
            {
                while (TryReadSvgFloat(d, ref i, out float y))
                {
                    cur = rel ? new(cur.X, cur.Y + y) : new(cur.X, y);
                    poly.Add(TransformPoint(cur, transform));
                }
                hasLastC2 = false;
                hasLastQ = false;
            }
            else if (uc == 'C' && poly is not null)
            {
                while (TryReadSvgFloat(d, ref i, out float x1))
                {
                    float y1 = ReadSvgFloat(d, ref i);
                    float x2 = ReadSvgFloat(d, ref i);
                    float y2 = ReadSvgFloat(d, ref i);
                    float x = ReadSvgFloat(d, ref i);
                    float y = ReadSvgFloat(d, ref i);
                    PointF p1 = rel ? new(cur.X + x1, cur.Y + y1) : new(x1, y1);
                    PointF p2 = rel ? new(cur.X + x2, cur.Y + y2) : new(x2, y2);
                    PointF p3 = rel ? new(cur.X + x, cur.Y + y) : new(x, y);
                    AppendCubicPolyline(poly, cur, p1, p2, p3, transform, 14);
                    cur = p3;
                    lastC2 = p2;
                    hasLastC2 = true;
                    hasLastQ = false;
                }
            }
            else if (uc == 'S' && poly is not null)
            {
                while (TryReadSvgFloat(d, ref i, out float x2))
                {
                    float y2 = ReadSvgFloat(d, ref i);
                    float x = ReadSvgFloat(d, ref i);
                    float y = ReadSvgFloat(d, ref i);
                    PointF p1 = hasLastC2 ? new((2 * cur.X) - lastC2.X, (2 * cur.Y) - lastC2.Y) : cur;
                    PointF p2 = rel ? new(cur.X + x2, cur.Y + y2) : new(x2, y2);
                    PointF p3 = rel ? new(cur.X + x, cur.Y + y) : new(x, y);
                    AppendCubicPolyline(poly, cur, p1, p2, p3, transform, 14);
                    cur = p3;
                    lastC2 = p2;
                    hasLastC2 = true;
                    hasLastQ = false;
                }
            }
            else if (uc == 'Q' && poly is not null)
            {
                while (TryReadSvgFloat(d, ref i, out float x1))
                {
                    float y1 = ReadSvgFloat(d, ref i);
                    float x = ReadSvgFloat(d, ref i);
                    float y = ReadSvgFloat(d, ref i);
                    PointF q1 = rel ? new(cur.X + x1, cur.Y + y1) : new(x1, y1);
                    PointF p3 = rel ? new(cur.X + x, cur.Y + y) : new(x, y);
                    AppendQuadraticPolyline(poly, cur, q1, p3, transform, 12);
                    cur = p3;
                    lastQ = q1;
                    hasLastQ = true;
                    hasLastC2 = false;
                }
            }
            else if (uc == 'T' && poly is not null)
            {
                while (TryReadSvgFloat(d, ref i, out float x))
                {
                    float y = ReadSvgFloat(d, ref i);
                    PointF q1 = hasLastQ ? new((2 * cur.X) - lastQ.X, (2 * cur.Y) - lastQ.Y) : cur;
                    PointF p3 = rel ? new(cur.X + x, cur.Y + y) : new(x, y);
                    AppendQuadraticPolyline(poly, cur, q1, p3, transform, 12);
                    cur = p3;
                    lastQ = q1;
                    hasLastQ = true;
                    hasLastC2 = false;
                }
            }
            else if (uc == 'A' && poly is not null)
            {
                while (TryReadSvgFloat(d, ref i, out float rx))
                {
                    _ = ReadSvgFloat(d, ref i); // ry
                    _ = ReadSvgFloat(d, ref i); // x-axis-rotation
                    _ = ReadSvgFloat(d, ref i); // large-arc-flag
                    _ = ReadSvgFloat(d, ref i); // sweep-flag
                    float x = ReadSvgFloat(d, ref i);
                    float y = ReadSvgFloat(d, ref i);
                    PointF p3 = rel ? new(cur.X + x, cur.Y + y) : new(x, y);
                    // Fallback: linear segment for arc commands.
                    if (rx > 0) poly.Add(TransformPoint(p3, transform));
                    cur = p3;
                    hasLastC2 = false;
                    hasLastQ = false;
                }
            }
            else if (uc == 'Z')
            {
                if (poly is { Count: > 1 })
                    poly.Add(poly[0]);
                cur = subStart;
                hasLastC2 = false;
                hasLastQ = false;
            }
            else
            {
                // Unsupported command: stop current command token consumption safely.
                hasLastC2 = false;
                hasLastQ = false;
            }
        }
    
        return result;
    }
    
    static void AppendCubicPolyline(List<PointF> poly, PointF p0, PointF p1, PointF p2, PointF p3, Matrix3x2 tr, int steps)
    {
        for (int s = 1; s <= steps; s++)
        {
            float t = s / (float)steps;
            float it = 1f - t;
            float x = it * it * it * p0.X + 3f * it * it * t * p1.X + 3f * it * t * t * p2.X + t * t * t * p3.X;
            float y = it * it * it * p0.Y + 3f * it * it * t * p1.Y + 3f * it * t * t * p2.Y + t * t * t * p3.Y;
            poly.Add(TransformPoint(new PointF(x, y), tr));
        }
    }
    
    static void AppendQuadraticPolyline(List<PointF> poly, PointF p0, PointF p1, PointF p2, Matrix3x2 tr, int steps)
    {
        for (int s = 1; s <= steps; s++)
        {
            float t = s / (float)steps;
            float it = 1f - t;
            float x = it * it * p0.X + 2f * it * t * p1.X + t * t * p2.X;
            float y = it * it * p0.Y + 2f * it * t * p1.Y + t * t * p2.Y;
            poly.Add(TransformPoint(new PointF(x, y), tr));
        }
    }
    
    static bool IsSvgCmd(char ch) => ch is 'M' or 'm' or 'L' or 'l' or 'H' or 'h' or 'V' or 'v' or 'C' or 'c' or 'S' or 's' or 'Q' or 'q' or 'T' or 't' or 'A' or 'a' or 'Z' or 'z';
    
    static void SkipSvgWs(string s, ref int i)
    {
        while (i < s.Length)
        {
            char ch = s[i];
            if (char.IsWhiteSpace(ch) || ch == ',') i++;
            else break;
        }
    }
    
    static bool TryReadSvgFloat(string s, ref int i, out float value)
    {
        SkipSvgWs(s, ref i);
        int start = i;
        if (i < s.Length && (s[i] == '+' || s[i] == '-')) i++;
        bool hasDigits = false;
        while (i < s.Length && char.IsDigit(s[i])) { i++; hasDigits = true; }
        if (i < s.Length && s[i] == '.')
        {
            i++;
            while (i < s.Length && char.IsDigit(s[i])) { i++; hasDigits = true; }
        }
        if (!hasDigits)
        {
            i = start;
            value = 0;
            return false;
        }
        if (i < s.Length && (s[i] == 'e' || s[i] == 'E'))
        {
            int exp = i++;
            if (i < s.Length && (s[i] == '+' || s[i] == '-')) i++;
            bool expDigits = false;
            while (i < s.Length && char.IsDigit(s[i])) { i++; expDigits = true; }
            if (!expDigits) i = exp;
        }
        var span = s.AsSpan(start, i - start);
        value = float.Parse(span, CultureInfo.InvariantCulture);
        return true;
    }
    
    static float ReadSvgFloat(string s, ref int i)
    {
        if (!TryReadSvgFloat(s, ref i, out float v))
            return 0f;
        return v;
    }
    
    static Matrix3x2 ParseSvgTransform(string transform)
    {
        transform = transform.Trim();
        if (transform.StartsWith("matrix(", StringComparison.OrdinalIgnoreCase) && transform.EndsWith(")", StringComparison.Ordinal))
        {
            string args = transform[7..^1];
            var vals = ParseSvgFloatList(args);
            if (vals.Count >= 6)
                return new Matrix3x2(vals[0], vals[1], vals[2], vals[3], vals[4], vals[5]);
        }
        if (transform.StartsWith("translate(", StringComparison.OrdinalIgnoreCase) && transform.EndsWith(")", StringComparison.Ordinal))
        {
            string args = transform[10..^1];
            var vals = ParseSvgFloatList(args);
            float tx = vals.Count > 0 ? vals[0] : 0f;
            float ty = vals.Count > 1 ? vals[1] : 0f;
            return Matrix3x2.CreateTranslation(tx, ty);
        }
        if (transform.StartsWith("scale(", StringComparison.OrdinalIgnoreCase) && transform.EndsWith(")", StringComparison.Ordinal))
        {
            string args = transform[6..^1];
            var vals = ParseSvgFloatList(args);
            float sx = vals.Count > 0 ? vals[0] : 1f;
            float sy = vals.Count > 1 ? vals[1] : sx;
            return Matrix3x2.CreateScale(sx, sy);
        }
        return Matrix3x2.Identity;
    }
    
    static List<float> ParseSvgFloatList(string text)
    {
        var vals = new List<float>(8);
        int i = 0;
        while (TryReadSvgFloat(text, ref i, out float v))
            vals.Add(v);
        return vals;
    }
    
    static RectF ParseViewBox(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return new RectF(0, 0, 0, 0);
        var vals = ParseSvgFloatList(s);
        if (vals.Count < 4)
            return new RectF(0, 0, 0, 0);
        return new RectF(vals[0], vals[1], vals[2], vals[3]);
    }
    
    static ColorF? ParseSvgColor(string value)
    {
        value = value.Trim();
        if (value.Equals("none", StringComparison.OrdinalIgnoreCase))
            return null;
        if (value.StartsWith('#'))
        {
            string hex = value[1..];
            if (hex.Length == 3)
            {
                byte r = Convert.ToByte(new string(hex[0], 2), 16);
                byte g = Convert.ToByte(new string(hex[1], 2), 16);
                byte b = Convert.ToByte(new string(hex[2], 2), 16);
                return ColorF.FromRgb(r, g, b);
            }
            if (hex.Length == 6)
            {
                byte r = Convert.ToByte(hex[0..2], 16);
                byte g = Convert.ToByte(hex[2..4], 16);
                byte b = Convert.ToByte(hex[4..6], 16);
                return ColorF.FromRgb(r, g, b);
            }
        }
        return ColorF.White;
    }
    
    static PointF TransformPoint(PointF p, Matrix3x2 tr)
    {
        var v = Vector2.Transform(new Vector2(p.X, p.Y), tr);
        return new PointF(v.X, v.Y);
    }
}
