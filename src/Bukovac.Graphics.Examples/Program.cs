using Bukovac.Graphics;
using System.Diagnostics;

var options = ParseArgs(args);
Directory.CreateDirectory(options.OutputDirectory);

IReadOnlyList<RasterizerKind> available = GraphicsConfig.GetAvailableRasterizers();
if (available.Count == 0)
{
    Console.WriteLine("No rasterizers available on this OS.");
    return;
}

var selectedRasterizers = new List<RasterizerKind>();
selectedRasterizers.AddRange(available);
if (options.Rasterizer.HasValue)
{
    Console.WriteLine($"Ignoring --rasterizer={options.Rasterizer.Value}. Rendering all available rasterizers per sample.");
}

Console.WriteLine($"Using rasterizers: {string.Join(", ", selectedRasterizers)}");
Console.WriteLine($"Saving format: {options.Format}");
Console.WriteLine($"Output: {options.OutputDirectory}");
Console.WriteLine($"Available rasterizers: {string.Join(", ", available)}");

string uiFamily = OperatingSystem.IsWindows()
    ? "Segoe UI"
    : OperatingSystem.IsMacOS()
        ? "Helvetica"
        : "Sans";
string monoFamily = OperatingSystem.IsWindows()
    ? "Consolas"
    : OperatingSystem.IsMacOS()
        ? "Menlo"
        : "Monospace";
string emojiFamily = OperatingSystem.IsWindows()
    ? "Segoe UI Emoji"
    : OperatingSystem.IsMacOS()
        ? "Apple Color Emoji"
        : "Noto Color Emoji";

FontSpec ui = new(uiFamily, 16);
FontSpec title = new(uiFamily, 26, FontWeight.Bold);
FontSpec timingFont = new(uiFamily, 14, FontWeight.SemiBold);

var samples = new List<(string Name, Action<Canvas, int, int> Render)>();
AddAllSamples(samples, ui, title, uiFamily, monoFamily, emojiFamily);

if (selectedRasterizers.Count > 0)
{
    string compareDir = options.OutputDirectory;
    Directory.CreateDirectory(compareDir);
    for (int i = 0; i < samples.Count; i++)
    {
        var sample = samples[i];
        string path = Path.Combine(compareDir, $"{sample.Name}.{Ext(options.Format)}");
        SaveComparisonImage(path, sample.Name, sample.Render, selectedRasterizers, options.Width, options.Height, options.Format, options.JpegQuality, ui, timingFont);
        Console.WriteLine($"Saved {path}");
    }
}

return;

static string Ext(ImageFileFormat format) => format switch
{
    ImageFileFormat.Png => "png",
    ImageFileFormat.Jpeg => "jpg",
    ImageFileFormat.Bmp => "bmp",
    ImageFileFormat.Gif => "gif",
    _ => "png",
};

static void SaveComparisonImage(string outputPath, string sampleName, Action<Canvas, int, int> render,
    IReadOnlyList<RasterizerKind> rasterizers, int width, int height, ImageFileFormat format, int jpegQuality, FontSpec labelFont, FontSpec timingFont)
{
    const InterpolationMode interpolationMode = InterpolationMode.HighQualityBicubic;
    const SmoothingMode smoothingMode = SmoothingMode.HighQuality;
    const PixelOffsetMode pixelOffsetMode = PixelOffsetMode.HighQuality;
    const CompositingQuality compositingQuality = CompositingQuality.HighQuality;

    var captures = new List<(RasterizerKind Rasterizer, int Width, int Height, byte[] Pixels, double Ms)>(rasterizers.Count);
    for (int i = 0; i < rasterizers.Count; i++)
    {
        var kind = rasterizers[i];
        using var canvas = new Canvas(kind);
        canvas.Initialize(width, height);
        ApplyHighestQuality(canvas, interpolationMode, smoothingMode, pixelOffsetMode, compositingQuality);

        canvas.BeginFrame();
        long start = Stopwatch.GetTimestamp();
        render(canvas, width, height);
        canvas.EndFrame();
        long end = Stopwatch.GetTimestamp();
        double ms = (end - start) * 1000.0 / Stopwatch.Frequency;

        if (canvas.TryCapturePixelsBgra(out int cw, out int ch, out byte[] px))
        {
            captures.Add((kind, cw, ch, px, ms));
        }
    }

    if (captures.Count == 0)
    {
        return;
    }

    const int pad = 20;
    const int labelHeight = 96;
    const int headerGap = 14;
    const int maxPerRow = 2;
    int columns = Math.Min(maxPerRow, captures.Count);
    int rows = (captures.Count + columns - 1) / columns;
    int outWidth = (columns * (width + pad)) + pad;
    int outHeight = pad + labelHeight + headerGap + (rows * (height + labelHeight + pad)) + pad;

    using var outCanvas = new Canvas(rasterizers[0]);
    outCanvas.Initialize(outWidth, outHeight);
    ApplyHighestQuality(outCanvas, interpolationMode, smoothingMode, pixelOffsetMode, compositingQuality);
    outCanvas.BeginFrame();
    outCanvas.Clear(ColorF.FromRgb(0x12, 0x12, 0x12));
    outCanvas.DrawString($"Bukovac.Graphics Renderers Comparision: {sampleName}", labelFont, new SolidBrush(ColorF.FromRgb(0xFA, 0xF0, 0xCC)), 18, 10);
    string qualityLabel =
        $"InterpolationMode={interpolationMode}, SmoothingMode={smoothingMode}, PixelOffsetMode={pixelOffsetMode}, CompositingQuality={compositingQuality}";
    outCanvas.DrawString(qualityLabel, timingFont, new SolidBrush(ColorF.FromRgb(0xB8, 0xD3, 0xFF)), 18, 44);

    for (int i = 0; i < captures.Count; i++)
    {
        var cap = captures[i];
        int col = i % columns;
        int row = i / columns;
        int x = pad + (col * (width + pad));
        int y = pad + labelHeight + headerGap + (row * (height + labelHeight + pad));

        outCanvas.FillRectangle(new SolidBrush(ColorF.FromArgb(255, 0x20, 0x20, 0x20)), x - 2, y - 2, width + 4, height + 4);
        var img = outCanvas.LoadImage(cap.Width, cap.Height, cap.Pixels);
        outCanvas.DrawImage(img, new RectF(x, y, width, height));
        outCanvas.DrawString(cap.Rasterizer.ToString(), labelFont, new SolidBrush(ColorF.FromRgb(0xD8, 0xE6, 0xFF)), x, y - 34);

        string msLabel = $"{cap.Ms:F2} ms";
        var msSize = outCanvas.MeasureString(msLabel, timingFont, TextFormatFlags.NoWrap, width);
        float msX = x + width - msSize.X - 14;
        float msY = y + height - msSize.Y - 12;
        DrawTimingBadge(outCanvas, msLabel, timingFont, msX, msY, msSize.X, msSize.Y);
    }

    outCanvas.EndFrame();
    outCanvas.SaveImage(outputPath, format, jpegQuality);
}

static void ApplyHighestQuality(Canvas canvas, InterpolationMode interpolationMode, SmoothingMode smoothingMode, PixelOffsetMode pixelOffsetMode, CompositingQuality compositingQuality)
{
    canvas.InterpolationMode = interpolationMode;
    canvas.SmoothingMode = smoothingMode;
    canvas.PixelOffsetMode = pixelOffsetMode;
    canvas.CompositingQuality = compositingQuality;
}

static void DrawTimingBadge(Canvas canvas, string label, FontSpec font, float x, float y, float w, float h)
{
    canvas.FillRoundedRectangle(new SolidBrush(ColorF.FromArgb(180, 0x08, 0x08, 0x0C)), x - 8, y - 5, w + 16, h + 10, 8);
    canvas.DrawRoundedRectangle(new Pen(ColorF.FromArgb(190, 0xB2, 0xC9, 0xFF), 1), x - 8, y - 5, w + 16, h + 10, 8);
    canvas.DrawString(label, font, new SolidBrush(ColorF.FromRgb(0xF0, 0xF6, 0xFF)), x, y);
}

static Options ParseArgs(string[] args)
{
    var options = new Options();
    foreach (string arg in args)
    {
        if (arg.StartsWith("--out=", StringComparison.OrdinalIgnoreCase))
        {
            options.OutputDirectory = arg[6..].Trim('"');
        }
        else if (arg.StartsWith("--format=", StringComparison.OrdinalIgnoreCase))
        {
            options.Format = ParseFormat(arg[9..]);
        }
        else if (arg.StartsWith("--width=", StringComparison.OrdinalIgnoreCase) && int.TryParse(arg[8..], out int width))
        {
            options.Width = Math.Max(64, width);
        }
        else if (arg.StartsWith("--height=", StringComparison.OrdinalIgnoreCase) && int.TryParse(arg[9..], out int height))
        {
            options.Height = Math.Max(64, height);
        }
        else if (arg.StartsWith("--quality=", StringComparison.OrdinalIgnoreCase) && int.TryParse(arg[10..], out int quality))
        {
            options.JpegQuality = Math.Clamp(quality, 1, 100);
        }
        else if (arg.StartsWith("--rasterizer=", StringComparison.OrdinalIgnoreCase))
        {
            string name = arg[13..];
            if (Enum.TryParse<RasterizerKind>(name, true, out var parsed))
                options.Rasterizer = parsed;
        }
    }

    return options;
}

static ImageFileFormat ParseFormat(string value)
{
    return value.ToLowerInvariant() switch
    {
        "png" => ImageFileFormat.Png,
        "jpg" or "jpeg" => ImageFileFormat.Jpeg,
        "bmp" => ImageFileFormat.Bmp,
        "gif" => ImageFileFormat.Gif,
        _ => throw new ArgumentException($"Unsupported format '{value}'."),
    };
}

sealed class Options
{
    public string OutputDirectory { get; set; } = Path.Combine(Environment.CurrentDirectory, "samples-out");
    public ImageFileFormat Format { get; set; } = ImageFileFormat.Png;
    public int Width { get; set; } = 960;
    public int Height { get; set; } = 540;
    public int JpegQuality { get; set; } = 90;
    public RasterizerKind? Rasterizer { get; set; }
}


