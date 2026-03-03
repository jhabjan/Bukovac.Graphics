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
using Bukovac.Graphics.Commands;

namespace Bukovac.Graphics.Rasterizers.Mac;

/// <summary>
/// macOS Metal rasterizer.
/// Executes a subset of draw commands directly through a Metal render pipeline,
/// with fallback to CoreGraphics when unsupported commands are present.
/// </summary>
[SupportedOSPlatform("macos")]
public sealed class MetalRasterizer : IRasterizer
{
    private const nuint MTLPixelFormatBGRA8Unorm = 80;
    private const nuint MTLPrimitiveTypeTriangle = 3;
    private const nuint MTLTextureUsageShaderRead = 1;
    private const nuint MTLTextureUsageRenderTarget = 4;
    private const nuint MTLLoadActionClear = 2;
    private const nuint MTLStoreActionStore = 1;

    private readonly IRasterizer _backend = new CoreGraphicsRasterizer();
    private readonly Stack<RenderState> _stateStack = new();

    private bool _isWindowTarget;
    private nint _nsView;
    private nint _metalDevice;
    private nint _commandQueue;
    private nint _metalLayer;
    private nint _uploadTexture;
    private nint _offscreenTexture;
    private nint _colorPipelineState;

    private int _width;
    private int _height;
    private byte[]? _lastPixels;
    private bool _supportsDirectPipeline;
    private bool _disposed;
    private RenderState _state = RenderState.Default;

    public void Initialize(NativeWindowHandle window, int width, int height)
    {
        if (window.Kind != "NSView")
        {
            throw new ArgumentException($"MetalRasterizer requires NSView, got '{window.Kind}'.");
        }

        _isWindowTarget = true;
        _nsView = window.Handle;
        _width = width;
        _height = height;

        EnsureMetal();
        AttachMetalLayerToView(width, height);
        _backend.InitializeBitmap(width, height);
    }

    public void InitializeBitmap(int width, int height)
    {
        _isWindowTarget = false;
        _nsView = 0;
        _width = width;
        _height = height;

        EnsureMetal();
        _backend.InitializeBitmap(width, height);
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        _backend.Resize(width, height);

        if (_metalLayer != 0)
        {
            nint selSetDrawableSize = ObjC.sel_registerName("setDrawableSize:");
            ObjC.Send(_metalLayer, selSetDrawableSize, new CGSize(width, height));
        }

        ReleaseTexture(ref _uploadTexture);
        ReleaseTexture(ref _offscreenTexture);
    }

    public void BeginFrame()
    {
        _backend.BeginFrame();
        _state = RenderState.Default;
        _stateStack.Clear();
    }

    public void EndFrame(ReadOnlySpan<DrawCommand> commands)
    {
        if (_metalDevice == 0 || _commandQueue == 0 || _width <= 0 || _height <= 0)
        {
            return;
        }

        if (_supportsDirectPipeline && CanRenderDirect(commands))
        {
            if (RenderDirect(commands))
            {
                return;
            }
        }

        // Fallback: preserve full feature parity through CoreGraphics command execution.
        _backend.EndFrame(commands);
        if (!_backend.TryCopyPixelsBgra(out int width, out int height, out byte[] bgra))
        {
            return;
        }

        _lastPixels = bgra;
        EnsureUploadTexture(width, height);
        UploadPixelsToTexture(_uploadTexture, width, height, bgra);

        nint commandBuffer = ObjC.Send(_commandQueue, ObjC.sel_registerName("commandBuffer"));
        if (commandBuffer == 0)
        {
            return;
        }

        nint targetTexture = 0;
        nint drawable = 0;

        if (_isWindowTarget && _metalLayer != 0)
        {
            drawable = ObjC.Send(_metalLayer, ObjC.sel_registerName("nextDrawable"));
            if (drawable != 0)
            {
                targetTexture = ObjC.Send(drawable, ObjC.sel_registerName("texture"));
            }
        }
        else
        {
            EnsureOffscreenTexture(width, height);
            targetTexture = _offscreenTexture;
        }

        if (targetTexture != 0)
        {
            EncodeBlit(commandBuffer, _uploadTexture, targetTexture, width, height);
            if (drawable != 0)
            {
                ObjC.Send(commandBuffer, ObjC.sel_registerName("presentDrawable:"), drawable);
            }
            ObjC.Send(commandBuffer, ObjC.sel_registerName("commit"));
        }
    }

    public Vector2 MeasureString(string text, FontSpec font, float maxWidth)
        => _backend.MeasureString(text, font, maxWidth);

    public Vector2 MeasureString(string text, FontSpec font, float maxWidth, TextFormatFlags flags)
        => _backend.MeasureString(text, font, maxWidth, flags);

    public float GetFontHeight(FontSpec font)
        => _backend.GetFontHeight(font);

    public float GetDpi()
        => _backend.GetDpi();

    public void SetDpi(float dpi)
        => _backend.SetDpi(dpi);

    public void ApplyQualitySettings(InterpolationMode interpolation, SmoothingMode smoothing,
        PixelOffsetMode pixelOffset, CompositingQuality compositing)
        => _backend.ApplyQualitySettings(interpolation, smoothing, pixelOffset, compositing);

    public void InitializeFromHdc(nint hdc, int width, int height)
        => _backend.InitializeFromHdc(hdc, width, height);

    public void InitializeFromHdc(nint hdc, int x, int y, int width, int height)
        => _backend.InitializeFromHdc(hdc, x, y, width, height);

    public bool TryCopyPixelsBgra(out int width, out int height, out byte[] bgraPixels)
    {
        if (_lastPixels is { Length: > 0 })
        {
            width = _width;
            height = _height;
            bgraPixels = _lastPixels;
            return true;
        }

        return _backend.TryCopyPixelsBgra(out width, out height, out bgraPixels);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _backend.Dispose();

        ReleaseTexture(ref _uploadTexture);
        ReleaseTexture(ref _offscreenTexture);
        ReleaseObj(ref _colorPipelineState);
        ReleaseObj(ref _metalLayer);
        ReleaseObj(ref _commandQueue);
        ReleaseObj(ref _metalDevice);
    }

    private void EnsureMetal()
    {
        if (_metalDevice != 0)
        {
            return;
        }

        _metalDevice = Metal.MTLCreateSystemDefaultDevice();
        if (_metalDevice == 0)
        {
            throw new PlatformNotSupportedException("Metal device is unavailable.");
        }

        _commandQueue = ObjC.Send(_metalDevice, ObjC.sel_registerName("newCommandQueue"));
        if (_commandQueue == 0)
        {
            throw new InvalidOperationException("Failed to create Metal command queue.");
        }

        _supportsDirectPipeline = TryCreateColorPipeline();
    }

    private void AttachMetalLayerToView(int width, int height)
    {
        nint layerClass = ObjC.objc_getClass("CAMetalLayer");
        nint layer = ObjC.Send(layerClass, ObjC.sel_registerName("layer"));
        if (layer == 0)
        {
            throw new InvalidOperationException("Failed to create CAMetalLayer.");
        }

        ObjC.Send(layer, ObjC.sel_registerName("setDevice:"), _metalDevice);
        ObjC.Send(layer, ObjC.sel_registerName("setPixelFormat:"), MTLPixelFormatBGRA8Unorm);
        ObjC.Send(layer, ObjC.sel_registerName("setFramebufferOnly:"), false);
        ObjC.Send(layer, ObjC.sel_registerName("setOpaque:"), false);
        ObjC.Send(layer, ObjC.sel_registerName("setDrawableSize:"), new CGSize(width, height));

        ObjC.Send(_nsView, ObjC.sel_registerName("setWantsLayer:"), true);
        ObjC.Send(_nsView, ObjC.sel_registerName("setLayer:"), layer);
        _metalLayer = layer;
    }

    private bool CanRenderDirect(ReadOnlySpan<DrawCommand> commands)
    {
        for (int i = 0; i < commands.Length; i++)
        {
            ref readonly DrawCommand cmd = ref commands[i];
            switch (cmd.Kind)
            {
                case DrawCommandKind.Clear:
                case DrawCommandKind.Save:
                case DrawCommandKind.Restore:
                case DrawCommandKind.SetTransform:
                case DrawCommandKind.ResetTransform:
                case DrawCommandKind.DrawLine:
                case DrawCommandKind.DrawRectangle:
                case DrawCommandKind.FillRectangle:
                case DrawCommandKind.DrawEllipse:
                case DrawCommandKind.FillEllipse:
                case DrawCommandKind.DrawRoundedRectangle:
                case DrawCommandKind.FillRoundedRectangle:
                    if ((cmd.Kind == DrawCommandKind.DrawLine ||
                         cmd.Kind == DrawCommandKind.DrawRectangle ||
                         cmd.Kind == DrawCommandKind.DrawEllipse ||
                         cmd.Kind == DrawCommandKind.DrawRoundedRectangle) &&
                        cmd.DashStyle != DashStyle.Solid)
                    {
                        return false;
                    }
                    break;

                default:
                    return false;
            }
        }

        return true;
    }

    private bool RenderDirect(ReadOnlySpan<DrawCommand> commands)
    {
        nint commandBuffer = ObjC.Send(_commandQueue, ObjC.sel_registerName("commandBuffer"));
        if (commandBuffer == 0)
        {
            return false;
        }

        nint targetTexture = 0;
        nint drawable = 0;

        if (_isWindowTarget && _metalLayer != 0)
        {
            drawable = ObjC.Send(_metalLayer, ObjC.sel_registerName("nextDrawable"));
            if (drawable == 0)
            {
                return false;
            }

            targetTexture = ObjC.Send(drawable, ObjC.sel_registerName("texture"));
        }
        else
        {
            EnsureOffscreenTexture(_width, _height);
            targetTexture = _offscreenTexture;
        }

        if (targetTexture == 0)
        {
            return false;
        }

        nint renderPass = ObjC.Send(ObjC.objc_getClass("MTLRenderPassDescriptor"), ObjC.sel_registerName("renderPassDescriptor"));
        if (renderPass == 0)
        {
            return false;
        }

        nint colorAttachments = ObjC.Send(renderPass, ObjC.sel_registerName("colorAttachments"));
        nint colorAttachment0 = ObjC.Send(colorAttachments, ObjC.sel_registerName("objectAtIndexedSubscript:"), (nuint)0);
        if (colorAttachment0 == 0)
        {
            return false;
        }

        ObjC.Send(colorAttachment0, ObjC.sel_registerName("setTexture:"), targetTexture);
        ObjC.Send(colorAttachment0, ObjC.sel_registerName("setLoadAction:"), MTLLoadActionClear);
        ObjC.Send(colorAttachment0, ObjC.sel_registerName("setStoreAction:"), MTLStoreActionStore);
        ObjC.Send(colorAttachment0, ObjC.sel_registerName("setClearColor:"), new MTLClearColor(0, 0, 0, 0));

        nint encoder = ObjC.Send(commandBuffer, ObjC.sel_registerName("renderCommandEncoderWithDescriptor:"), renderPass);
        if (encoder == 0)
        {
            return false;
        }

        ObjC.Send(encoder, ObjC.sel_registerName("setRenderPipelineState:"), _colorPipelineState);
        ObjC.Send(encoder, ObjC.sel_registerName("setScissorRect:"), new MTLScissorRect(0, 0, (nuint)_width, (nuint)_height));

        for (int i = 0; i < commands.Length; i++)
        {
            ref readonly DrawCommand cmd = ref commands[i];

            switch (cmd.Kind)
            {
                case DrawCommandKind.Clear:
                    DrawFilledRect(encoder, new RectF(0, 0, _width, _height), cmd.Color);
                    break;
                case DrawCommandKind.Save:
                    _stateStack.Push(_state);
                    break;
                case DrawCommandKind.Restore:
                    _state = _stateStack.Count > 0 ? _stateStack.Pop() : RenderState.Default;
                    break;
                case DrawCommandKind.SetTransform:
                    _state.Transform = cmd.Transform;
                    break;
                case DrawCommandKind.ResetTransform:
                    _state.Transform = Matrix3x2.Identity;
                    break;
                case DrawCommandKind.DrawLine:
                    DrawLine(encoder, cmd.P1, cmd.P2, MathF.Max(1f, cmd.StrokeWidth), cmd.Color);
                    break;
                case DrawCommandKind.DrawRectangle:
                    DrawRectangle(encoder, cmd.Rect, MathF.Max(1f, cmd.StrokeWidth), cmd.Color);
                    break;
                case DrawCommandKind.FillRectangle:
                    DrawFilledRect(encoder, cmd.Rect, cmd.Color);
                    break;
                case DrawCommandKind.DrawEllipse:
                    DrawEllipse(encoder, cmd.Rect, MathF.Max(1f, cmd.StrokeWidth), cmd.Color, fill: false);
                    break;
                case DrawCommandKind.FillEllipse:
                    DrawEllipse(encoder, cmd.Rect, 1f, cmd.Color, fill: true);
                    break;
                case DrawCommandKind.DrawRoundedRectangle:
                    DrawRoundedRectangle(encoder, cmd.Rect, cmd.CornerRadius, MathF.Max(1f, cmd.StrokeWidth), cmd.Color, fill: false);
                    break;
                case DrawCommandKind.FillRoundedRectangle:
                    DrawRoundedRectangle(encoder, cmd.Rect, cmd.CornerRadius, 1f, cmd.Color, fill: true);
                    break;
            }
        }

        ObjC.Send(encoder, ObjC.sel_registerName("endEncoding"));

        if (drawable != 0)
        {
            ObjC.Send(commandBuffer, ObjC.sel_registerName("presentDrawable:"), drawable);
        }

        ObjC.Send(commandBuffer, ObjC.sel_registerName("commit"));

        if (!_isWindowTarget)
        {
            ObjC.Send(commandBuffer, ObjC.sel_registerName("waitUntilCompleted"));
            _lastPixels = ReadTexturePixels(targetTexture, _width, _height);
        }
        else
        {
            _lastPixels = null;
        }

        return true;
    }

    private bool TryCreateColorPipeline()
    {
        const string source = """
            #include <metal_stdlib>
            using namespace metal;

            struct VertexIn
            {
                float2 position;
                float4 color;
            };

            struct Uniforms
            {
                float2 viewport;
            };

            struct VertexOut
            {
                float4 position [[position]];
                float4 color;
            };

            vertex VertexOut vs_main(uint vid [[vertex_id]],
                                     constant VertexIn* vertices [[buffer(0)]],
                                     constant Uniforms& uniforms [[buffer(1)]])
            {
                VertexOut outv;
                float2 p = vertices[vid].position;
                float2 ndc;
                ndc.x = (p.x / uniforms.viewport.x) * 2.0 - 1.0;
                ndc.y = 1.0 - (p.y / uniforms.viewport.y) * 2.0;
                outv.position = float4(ndc, 0.0, 1.0);
                outv.color = vertices[vid].color;
                return outv;
            }

            fragment float4 fs_main(VertexOut inV [[stage_in]])
            {
                return inV.color;
            }
            """;

        nint library = 0;
        nint vertexFn = 0;
        nint fragmentFn = 0;
        nint descriptor = 0;
        nint pipeline = 0;
        nint sourceStr = 0;
        nint vertexName = 0;
        nint fragmentName = 0;

        try
        {
            sourceStr = CF.CFStringCreateWithCString(0, source, CF.kCFStringEncodingUTF8);
            vertexName = CF.CFStringCreateWithCString(0, "vs_main", CF.kCFStringEncodingUTF8);
            fragmentName = CF.CFStringCreateWithCString(0, "fs_main", CF.kCFStringEncodingUTF8);
            if (sourceStr == 0 || vertexName == 0 || fragmentName == 0)
            {
                return false;
            }

            unsafe
            {
                nint* error = stackalloc nint[1];
                error[0] = 0;
                library = ObjC.Send(_metalDevice, ObjC.sel_registerName("newLibraryWithSource:options:error:"), sourceStr, 0, (nint)error);
                if (library == 0)
                {
                    return false;
                }

                vertexFn = ObjC.Send(library, ObjC.sel_registerName("newFunctionWithName:"), vertexName);
                fragmentFn = ObjC.Send(library, ObjC.sel_registerName("newFunctionWithName:"), fragmentName);
                if (vertexFn == 0 || fragmentFn == 0)
                {
                    return false;
                }

                nint descriptorClass = ObjC.objc_getClass("MTLRenderPipelineDescriptor");
                descriptor = ObjC.Send(ObjC.Send(descriptorClass, ObjC.sel_registerName("alloc")), ObjC.sel_registerName("init"));
                if (descriptor == 0)
                {
                    return false;
                }

                ObjC.Send(descriptor, ObjC.sel_registerName("setVertexFunction:"), vertexFn);
                ObjC.Send(descriptor, ObjC.sel_registerName("setFragmentFunction:"), fragmentFn);

                nint colorAttachments = ObjC.Send(descriptor, ObjC.sel_registerName("colorAttachments"));
                nint attachment0 = ObjC.Send(colorAttachments, ObjC.sel_registerName("objectAtIndexedSubscript:"), (nuint)0);
                ObjC.Send(attachment0, ObjC.sel_registerName("setPixelFormat:"), MTLPixelFormatBGRA8Unorm);
                ObjC.Send(attachment0, ObjC.sel_registerName("setBlendingEnabled:"), true);
                ObjC.Send(attachment0, ObjC.sel_registerName("setRgbBlendOperation:"), (nuint)0);
                ObjC.Send(attachment0, ObjC.sel_registerName("setAlphaBlendOperation:"), (nuint)0);
                ObjC.Send(attachment0, ObjC.sel_registerName("setSourceRGBBlendFactor:"), (nuint)4);
                ObjC.Send(attachment0, ObjC.sel_registerName("setDestinationRGBBlendFactor:"), (nuint)5);
                ObjC.Send(attachment0, ObjC.sel_registerName("setSourceAlphaBlendFactor:"), (nuint)4);
                ObjC.Send(attachment0, ObjC.sel_registerName("setDestinationAlphaBlendFactor:"), (nuint)5);

                error[0] = 0;
                pipeline = ObjC.Send(_metalDevice, ObjC.sel_registerName("newRenderPipelineStateWithDescriptor:error:"), descriptor, (nint)error);
                if (pipeline == 0)
                {
                    return false;
                }
            }

            _colorPipelineState = pipeline;
            pipeline = 0;
            return true;
        }
        finally
        {
            ReleaseObj(ref pipeline);
            ReleaseObj(ref descriptor);
            ReleaseObj(ref vertexFn);
            ReleaseObj(ref fragmentFn);
            ReleaseObj(ref library);

            if (sourceStr != 0)
            {
                CF.CFRelease(sourceStr);
            }

            if (vertexName != 0)
            {
                CF.CFRelease(vertexName);
            }

            if (fragmentName != 0)
            {
                CF.CFRelease(fragmentName);
            }
        }
    }

    private void DrawFilledRect(nint encoder, RectF rect, ColorF color)
    {
        PointF p0 = TransformPoint(new PointF(rect.X, rect.Y));
        PointF p1 = TransformPoint(new PointF(rect.Right, rect.Y));
        PointF p2 = TransformPoint(new PointF(rect.Right, rect.Bottom));
        PointF p3 = TransformPoint(new PointF(rect.X, rect.Bottom));

        Span<VertexColor> vertices = stackalloc VertexColor[6];
        ColorToVertices(vertices, color, p0, p1, p2, p0, p2, p3);
        DrawTriangles(encoder, vertices);
    }

    private void DrawRectangle(nint encoder, RectF rect, float strokeWidth, ColorF color)
    {
        PointF a = new(rect.X, rect.Y);
        PointF b = new(rect.Right, rect.Y);
        PointF c = new(rect.Right, rect.Bottom);
        PointF d = new(rect.X, rect.Bottom);

        DrawLine(encoder, a, b, strokeWidth, color);
        DrawLine(encoder, b, c, strokeWidth, color);
        DrawLine(encoder, c, d, strokeWidth, color);
        DrawLine(encoder, d, a, strokeWidth, color);
    }

    private void DrawLine(nint encoder, PointF a, PointF b, float strokeWidth, ColorF color)
    {
        PointF p0 = TransformPoint(a);
        PointF p1 = TransformPoint(b);

        float dx = p1.X - p0.X;
        float dy = p1.Y - p0.Y;
        float len = MathF.Sqrt((dx * dx) + (dy * dy));
        if (len <= 0.001f)
        {
            return;
        }

        float inv = 0.5f * strokeWidth / len;
        float ox = -dy * inv;
        float oy = dx * inv;

        PointF v0 = new(p0.X + ox, p0.Y + oy);
        PointF v1 = new(p0.X - ox, p0.Y - oy);
        PointF v2 = new(p1.X - ox, p1.Y - oy);
        PointF v3 = new(p1.X + ox, p1.Y + oy);

        Span<VertexColor> vertices = stackalloc VertexColor[6];
        ColorToVertices(vertices, color, v0, v1, v2, v0, v2, v3);
        DrawTriangles(encoder, vertices);
    }

    private void DrawEllipse(nint encoder, RectF rect, float strokeWidth, ColorF color, bool fill)
    {
        int segments = 64;
        float cx = rect.X + (rect.Width * 0.5f);
        float cy = rect.Y + (rect.Height * 0.5f);
        float rx = rect.Width * 0.5f;
        float ry = rect.Height * 0.5f;

        if (fill)
        {
            var vertices = new VertexColor[segments * 3];
            PointF center = TransformPoint(new PointF(cx, cy));
            int idx = 0;
            for (int i = 0; i < segments; i++)
            {
                float t0 = (i / (float)segments) * (MathF.PI * 2f);
                float t1 = ((i + 1) / (float)segments) * (MathF.PI * 2f);

                PointF p0 = TransformPoint(new PointF(cx + (MathF.Cos(t0) * rx), cy + (MathF.Sin(t0) * ry)));
                PointF p1 = TransformPoint(new PointF(cx + (MathF.Cos(t1) * rx), cy + (MathF.Sin(t1) * ry)));

                vertices[idx++] = new VertexColor(center, color);
                vertices[idx++] = new VertexColor(p0, color);
                vertices[idx++] = new VertexColor(p1, color);
            }

            DrawTriangles(encoder, vertices);
            return;
        }

        float innerRx = MathF.Max(0, rx - (strokeWidth * 0.5f));
        float innerRy = MathF.Max(0, ry - (strokeWidth * 0.5f));
        float outerRx = rx + (strokeWidth * 0.5f);
        float outerRy = ry + (strokeWidth * 0.5f);

        var ringVertices = new VertexColor[segments * 6];
        int ringIdx = 0;
        for (int i = 0; i < segments; i++)
        {
            float t0 = (i / (float)segments) * (MathF.PI * 2f);
            float t1 = ((i + 1) / (float)segments) * (MathF.PI * 2f);

            PointF o0 = TransformPoint(new PointF(cx + (MathF.Cos(t0) * outerRx), cy + (MathF.Sin(t0) * outerRy)));
            PointF o1 = TransformPoint(new PointF(cx + (MathF.Cos(t1) * outerRx), cy + (MathF.Sin(t1) * outerRy)));
            PointF i0 = TransformPoint(new PointF(cx + (MathF.Cos(t0) * innerRx), cy + (MathF.Sin(t0) * innerRy)));
            PointF i1 = TransformPoint(new PointF(cx + (MathF.Cos(t1) * innerRx), cy + (MathF.Sin(t1) * innerRy)));

            ringVertices[ringIdx++] = new VertexColor(o0, color);
            ringVertices[ringIdx++] = new VertexColor(i0, color);
            ringVertices[ringIdx++] = new VertexColor(i1, color);

            ringVertices[ringIdx++] = new VertexColor(o0, color);
            ringVertices[ringIdx++] = new VertexColor(i1, color);
            ringVertices[ringIdx++] = new VertexColor(o1, color);
        }

        DrawTriangles(encoder, ringVertices);
    }

    private void DrawRoundedRectangle(nint encoder, RectF rect, float cornerRadius, float strokeWidth, ColorF color, bool fill)
    {
        float r = MathF.Min(cornerRadius, MathF.Min(rect.Width, rect.Height) * 0.5f);
        int arcSegments = 12;
        var points = new List<PointF>(arcSegments * 4);

        AppendCorner(points, rect.Right - r, rect.Y + r, r, -MathF.PI / 2f, 0f, arcSegments);
        AppendCorner(points, rect.Right - r, rect.Bottom - r, r, 0f, MathF.PI / 2f, arcSegments);
        AppendCorner(points, rect.X + r, rect.Bottom - r, r, MathF.PI / 2f, MathF.PI, arcSegments);
        AppendCorner(points, rect.X + r, rect.Y + r, r, MathF.PI, MathF.PI * 1.5f, arcSegments);

        if (fill)
        {
            if (points.Count < 3)
            {
                return;
            }

            PointF center = TransformPoint(rect.Center);
            var vertices = new VertexColor[points.Count * 3];
            int idx = 0;
            for (int i = 0; i < points.Count; i++)
            {
                PointF p0 = TransformPoint(points[i]);
                PointF p1 = TransformPoint(points[(i + 1) % points.Count]);

                vertices[idx++] = new VertexColor(center, color);
                vertices[idx++] = new VertexColor(p0, color);
                vertices[idx++] = new VertexColor(p1, color);
            }

            DrawTriangles(encoder, vertices);
            return;
        }

        for (int i = 0; i < points.Count; i++)
        {
            DrawLine(encoder, points[i], points[(i + 1) % points.Count], strokeWidth, color);
        }
    }

    private unsafe void DrawTriangles(nint encoder, ReadOnlySpan<VertexColor> vertices)
    {
        if (vertices.Length == 0)
        {
            return;
        }

        Uniforms uniforms = new(_width, _height);
        fixed (VertexColor* pVertices = vertices)
        {
            ObjC.Send(encoder, ObjC.sel_registerName("setVertexBytes:length:atIndex:"), (nint)pVertices, (nuint)(vertices.Length * sizeof(VertexColor)), (nuint)0);
        }

        ObjC.Send(encoder, ObjC.sel_registerName("setVertexBytes:length:atIndex:"), (nint)(&uniforms), (nuint)sizeof(Uniforms), (nuint)1);
        ObjC.Send(encoder, ObjC.sel_registerName("drawPrimitives:vertexStart:vertexCount:"), MTLPrimitiveTypeTriangle, (nuint)0, (nuint)vertices.Length);
    }

    private PointF TransformPoint(PointF p)
    {
        Vector2 t = Vector2.Transform(new Vector2(p.X, p.Y), _state.Transform);
        return new PointF(t.X, t.Y);
    }

    private static void AppendCorner(List<PointF> points, float cx, float cy, float radius, float start, float end, int segments)
    {
        if (radius <= 0.001f)
        {
            points.Add(new PointF(cx, cy));
            return;
        }

        for (int i = 0; i <= segments; i++)
        {
            float t = start + ((end - start) * (i / (float)segments));
            points.Add(new PointF(cx + (MathF.Cos(t) * radius), cy + (MathF.Sin(t) * radius)));
        }
    }

    private static void ColorToVertices(Span<VertexColor> vertices, ColorF color,
        PointF p0, PointF p1, PointF p2, PointF p3, PointF p4, PointF p5)
    {
        vertices[0] = new VertexColor(p0, color);
        vertices[1] = new VertexColor(p1, color);
        vertices[2] = new VertexColor(p2, color);
        vertices[3] = new VertexColor(p3, color);
        vertices[4] = new VertexColor(p4, color);
        vertices[5] = new VertexColor(p5, color);
    }

    private void EnsureUploadTexture(int width, int height)
    {
        if (_uploadTexture != 0)
        {
            return;
        }

        _uploadTexture = CreateTexture(width, height, MTLTextureUsageShaderRead);
    }

    private void EnsureOffscreenTexture(int width, int height)
    {
        if (_offscreenTexture != 0)
        {
            return;
        }

        _offscreenTexture = CreateTexture(width, height, MTLTextureUsageRenderTarget | MTLTextureUsageShaderRead);
    }

    private nint CreateTexture(int width, int height, nuint usage)
    {
        nint textureDescriptorClass = ObjC.objc_getClass("MTLTextureDescriptor");
        nint descriptor = ObjC.Send(
            textureDescriptorClass,
            ObjC.sel_registerName("texture2DDescriptorWithPixelFormat:width:height:mipmapped:"),
            MTLPixelFormatBGRA8Unorm,
            (nuint)width,
            (nuint)height,
            false);
        if (descriptor == 0)
        {
            return 0;
        }

        ObjC.Send(descriptor, ObjC.sel_registerName("setUsage:"), usage);
        nint texture = ObjC.Send(_metalDevice, ObjC.sel_registerName("newTextureWithDescriptor:"), descriptor);
        return texture;
    }

    private static unsafe void UploadPixelsToTexture(nint texture, int width, int height, byte[] bgraPixels)
    {
        if (texture == 0)
        {
            return;
        }

        nint selReplace = ObjC.sel_registerName("replaceRegion:mipmapLevel:withBytes:bytesPerRow:");
        MTLRegion region = MTLRegion.Make2D(0, 0, width, height);
        fixed (byte* p = bgraPixels)
        {
            ObjC.Send(texture, selReplace, region, 0, (nint)p, (nuint)(width * 4));
        }
    }

    private static void EncodeBlit(nint commandBuffer, nint sourceTexture, nint destinationTexture, int width, int height)
    {
        if (sourceTexture == 0 || destinationTexture == 0)
        {
            return;
        }

        nint encoder = ObjC.Send(commandBuffer, ObjC.sel_registerName("blitCommandEncoder"));
        if (encoder == 0)
        {
            return;
        }

        nint selCopy = ObjC.sel_registerName("copyFromTexture:sourceSlice:sourceLevel:sourceOrigin:sourceSize:toTexture:destinationSlice:destinationLevel:destinationOrigin:");
        ObjC.Send(
            encoder,
            selCopy,
            sourceTexture,
            0,
            0,
            new MTLOrigin(0, 0, 0),
            new MTLSize((nuint)width, (nuint)height, 1),
            destinationTexture,
            0,
            0,
            new MTLOrigin(0, 0, 0));

        ObjC.Send(encoder, ObjC.sel_registerName("endEncoding"));
    }

    private static byte[] ReadTexturePixels(nint texture, int width, int height)
    {
        byte[] pixels = new byte[Math.Max(1, width * height * 4)];
        if (texture == 0 || width <= 0 || height <= 0)
        {
            return pixels;
        }

        unsafe
        {
            fixed (byte* p = pixels)
            {
                ObjC.SendVoid(
                    texture,
                    ObjC.sel_registerName("getBytes:bytesPerRow:fromRegion:mipmapLevel:"),
                    (nint)p,
                    (nuint)(width * 4),
                    MTLRegion.Make2D(0, 0, width, height),
                    0);
            }
        }

        return pixels;
    }

    private static void ReleaseTexture(ref nint texture)
    {
        ReleaseObj(ref texture);
    }

    private static void ReleaseObj(ref nint handle)
    {
        if (handle == 0)
        {
            return;
        }

        ObjC.Send(handle, ObjC.sel_registerName("release"));
        handle = 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VertexColor
    {
        public float X;
        public float Y;
        public float R;
        public float G;
        public float B;
        public float A;

        public VertexColor(PointF point, ColorF color)
        {
            X = point.X;
            Y = point.Y;
            R = color.R;
            G = color.G;
            B = color.B;
            A = color.A;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Uniforms
    {
        public float ViewportWidth;
        public float ViewportHeight;

        public Uniforms(float viewportWidth, float viewportHeight)
        {
            ViewportWidth = viewportWidth;
            ViewportHeight = viewportHeight;
        }
    }

    private struct RenderState
    {
        public Matrix3x2 Transform;

        public static RenderState Default => new()
        {
            Transform = Matrix3x2.Identity,
        };
    }
}
