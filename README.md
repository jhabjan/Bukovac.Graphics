# Bukovac.Graphics

**Cross-platform 2D graphics library for .NET**, with multiple native rasterizer backends per OS.

## Highlights

- Works across Windows, Linux, and macOS with a single `Canvas` API.
- Renders both off-screen bitmaps and directly to on-screen native window surfaces.
- Supports both CPU and GPU rendering backends:
- CPU backends: GDI (Windows), Cairo (Linux), CoreGraphics (macOS)
- GPU backends: Direct2D/OpenGL (Windows), OpenGL (Linux), Metal (macOS)
- NativeAOT-friendly and trimming-friendly design (switch-based factory, no reflection-heavy backend discovery).
- Full 2D drawing stack: shapes, text, images, transforms, clipping, and save/restore state.
- Off-screen rendering and export to `png`, `jpg`, `bmp`, and `gif`.
- Includes 50+ samples, including side-by-side backend comparison renders.

## Repository Layout

- `src/Bukovac.Graphics` - core library
- `src/Bukovac.Graphics.Examples` - console sample app that renders demo outputs
- `Bukovac.Graphics.sln` - solution file

## Requirements

- .NET 10 SDK (`net10.0`)
- Native graphics dependencies available on your OS for the backend(s) you want to use

## Build

```powershell
dotnet restore
dotnet build Bukovac.Graphics.sln -c Release
```

## Run Samples

```powershell
dotnet run --project src/Bukovac.Graphics.Examples
```

By default, outputs are written to:

- `./samples-out` (current working directory)

You can customize output:

```powershell
dotnet run --project src/Bukovac.Graphics.Examples -- --out=./out --format=png --width=960 --height=540
```

Supported CLI arguments:

- `--out=<path>`
- `--format=png|jpg|bmp|gif`
- `--width=<int>`
- `--height=<int>`
- `--quality=<1-100>` (JPEG only)
- `--rasterizer=<name>` (currently parsed, but examples render all available rasterizers)

## Basic Library Usage

```csharp
using Bukovac.Graphics;

using var canvas = new Canvas(RasterizerKind.Default);
canvas.Initialize(800, 600);

canvas.BeginFrame();
canvas.Clear(ColorF.FromRgb(30, 30, 30));
canvas.FillRectangle(new SolidBrush(ColorF.FromRgb(80, 170, 255)), 80, 80, 240, 140);
canvas.DrawString("Hello Bukovac.Graphics", new FontSpec("Segoe UI", 24), new SolidBrush(ColorF.White), 90, 130);
canvas.EndFrame();

canvas.SaveImage("hello.png", ImageFileFormat.Png);
```

## Rasterizer Selection

- Use `new Canvas()` or `RasterizerKind.Default` to auto-select by OS.
- Set globally with `GraphicsConfig.RasterizerKind`.
- Enumerate available backends with `GraphicsConfig.GetAvailableRasterizers()`.

## License

This project is licensed under the GNU General Public License v3.0 or later (GPL-3.0-or-later).

If you distribute this project or derivative works, include the GPLv3 license text and preserve copyright/license headers.

## Name

The library is named after **Vlaho Bukovac** (1855-1922), one of the most prominent Croatian painters and a key figure of Croatian modern art.
