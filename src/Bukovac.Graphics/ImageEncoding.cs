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

using System.Buffers.Binary;

namespace Bukovac.Graphics;

public enum ImageFileFormat
{
    Png,
    Jpeg,
    Bmp,
    Gif,
}

internal static class ImageEncoding
{
    private static readonly int[] ZigZag =
    [
         0,  1,  5,  6, 14, 15, 27, 28,
         2,  4,  7, 13, 16, 26, 29, 42,
         3,  8, 12, 17, 25, 30, 41, 43,
         9, 11, 18, 24, 31, 40, 44, 53,
        10, 19, 23, 32, 39, 45, 52, 54,
        20, 22, 33, 38, 46, 51, 55, 60,
        21, 34, 37, 47, 50, 56, 59, 61,
        35, 36, 48, 49, 57, 58, 62, 63,
    ];

    private static readonly byte[] LumaQuantBase =
    [
        16,11,10,16,24,40,51,61,
        12,12,14,19,26,58,60,55,
        14,13,16,24,40,57,69,56,
        14,17,22,29,51,87,80,62,
        18,22,37,56,68,109,103,77,
        24,35,55,64,81,104,113,92,
        49,64,78,87,103,121,120,101,
        72,92,95,98,112,100,103,99,
    ];

    private static readonly byte[] ChromaQuantBase =
    [
        17,18,24,47,99,99,99,99,
        18,21,26,66,99,99,99,99,
        24,26,56,99,99,99,99,99,
        47,66,99,99,99,99,99,99,
        99,99,99,99,99,99,99,99,
        99,99,99,99,99,99,99,99,
        99,99,99,99,99,99,99,99,
        99,99,99,99,99,99,99,99,
    ];

    // Number of codes for each bit length 1..16 (index 0 unused)
    private static readonly byte[] DcLumaBits = [0,0,1,5,1,1,1,1,1,1,1,0,0,0,0,0,0];
    private static readonly byte[] DcLumaVals = [0,1,2,3,4,5,6,7,8,9,10,11];

    private static readonly byte[] AcLumaBits = [0,0,2,1,3,3,2,4,3,5,5,4,4,0,0,1,0x7D];
    private static readonly byte[] AcLumaVals =
    [
        0x01,0x02,0x03,0x00,0x04,0x11,0x05,0x12,0x21,0x31,0x41,0x06,0x13,0x51,0x61,0x07,
        0x22,0x71,0x14,0x32,0x81,0x91,0xA1,0x08,0x23,0x42,0xB1,0xC1,0x15,0x52,0xD1,0xF0,
        0x24,0x33,0x62,0x72,0x82,0x09,0x0A,0x16,0x17,0x18,0x19,0x1A,0x25,0x26,0x27,0x28,
        0x29,0x2A,0x34,0x35,0x36,0x37,0x38,0x39,0x3A,0x43,0x44,0x45,0x46,0x47,0x48,0x49,
        0x4A,0x53,0x54,0x55,0x56,0x57,0x58,0x59,0x5A,0x63,0x64,0x65,0x66,0x67,0x68,0x69,
        0x6A,0x73,0x74,0x75,0x76,0x77,0x78,0x79,0x7A,0x83,0x84,0x85,0x86,0x87,0x88,0x89,
        0x8A,0x92,0x93,0x94,0x95,0x96,0x97,0x98,0x99,0x9A,0xA2,0xA3,0xA4,0xA5,0xA6,0xA7,
        0xA8,0xA9,0xAA,0xB2,0xB3,0xB4,0xB5,0xB6,0xB7,0xB8,0xB9,0xBA,0xC2,0xC3,0xC4,0xC5,
        0xC6,0xC7,0xC8,0xC9,0xCA,0xD2,0xD3,0xD4,0xD5,0xD6,0xD7,0xD8,0xD9,0xDA,0xE1,0xE2,
        0xE3,0xE4,0xE5,0xE6,0xE7,0xE8,0xE9,0xEA,0xF1,0xF2,0xF3,0xF4,0xF5,0xF6,0xF7,0xF8,
        0xF9,0xFA,
    ];

    private static readonly byte[] DcChromaBits = [0,0,3,1,1,1,1,1,1,1,1,1,0,0,0,0,0];
    private static readonly byte[] DcChromaVals = [0,1,2,3,4,5,6,7,8,9,10,11];

    private static readonly byte[] AcChromaBits = [0,0,2,1,2,4,4,3,4,7,5,4,4,0,1,2,0x77];
    private static readonly byte[] AcChromaVals =
    [
        0x00,0x01,0x02,0x03,0x11,0x04,0x05,0x21,0x31,0x06,0x12,0x41,0x51,0x07,0x61,0x71,
        0x13,0x22,0x32,0x81,0x08,0x14,0x42,0x91,0xA1,0xB1,0xC1,0x09,0x23,0x33,0x52,0xF0,
        0x15,0x62,0x72,0xD1,0x0A,0x16,0x24,0x34,0xE1,0x25,0xF1,0x17,0x18,0x19,0x1A,0x26,
        0x27,0x28,0x29,0x2A,0x35,0x36,0x37,0x38,0x39,0x3A,0x43,0x44,0x45,0x46,0x47,0x48,
        0x49,0x4A,0x53,0x54,0x55,0x56,0x57,0x58,0x59,0x5A,0x63,0x64,0x65,0x66,0x67,0x68,
        0x69,0x6A,0x73,0x74,0x75,0x76,0x77,0x78,0x79,0x7A,0x82,0x83,0x84,0x85,0x86,0x87,
        0x88,0x89,0x8A,0x92,0x93,0x94,0x95,0x96,0x97,0x98,0x99,0x9A,0xA2,0xA3,0xA4,0xA5,
        0xA6,0xA7,0xA8,0xA9,0xAA,0xB2,0xB3,0xB4,0xB5,0xB6,0xB7,0xB8,0xB9,0xBA,0xC2,0xC3,
        0xC4,0xC5,0xC6,0xC7,0xC8,0xC9,0xCA,0xD2,0xD3,0xD4,0xD5,0xD6,0xD7,0xD8,0xD9,0xDA,
        0xE2,0xE3,0xE4,0xE5,0xE6,0xE7,0xE8,0xE9,0xEA,0xF2,0xF3,0xF4,0xF5,0xF6,0xF7,0xF8,
        0xF9,0xFA,
    ];

    private static readonly double[,] Cosine = BuildCosineTable();

    public static ImageFileFormat DetectFormatFromPath(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".png" => ImageFileFormat.Png,
            ".jpg" or ".jpeg" => ImageFileFormat.Jpeg,
            ".bmp" => ImageFileFormat.Bmp,
            ".gif" => ImageFileFormat.Gif,
            _ => throw new NotSupportedException($"Unsupported file extension '{ext}'. Use png, jpg/jpeg, bmp, or gif."),
        };
    }

    public static void Save(string path, ImageFileFormat format, int width, int height, byte[] bgraPixels, int jpegQuality)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Image dimensions must be positive.");
        if (bgraPixels.Length < width * height * 4)
            throw new ArgumentException("Pixel buffer is too small.");

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);

        using FileStream fs = File.Create(path);
        switch (format)
        {
            case ImageFileFormat.Png:
                WritePng(fs, width, height, bgraPixels);
                break;
            case ImageFileFormat.Bmp:
                WriteBmp(fs, width, height, bgraPixels);
                break;
            case ImageFileFormat.Gif:
                WriteGif(fs, width, height, bgraPixels);
                break;
            case ImageFileFormat.Jpeg:
                WriteJpeg(fs, width, height, bgraPixels, jpegQuality);
                break;
            default:
                throw new NotSupportedException($"Unsupported format: {format}");
        }
    }

    private static void WriteBmp(Stream output, int width, int height, byte[] bgra)
    {
        int pixelBytes = width * height * 4;
        int fileSize = 14 + 40 + pixelBytes;

        Span<byte> fileHeader = stackalloc byte[14];
        fileHeader[0] = (byte)'B';
        fileHeader[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(fileHeader[2..], fileSize);
        BinaryPrimitives.WriteInt32LittleEndian(fileHeader[10..], 14 + 40);
        output.Write(fileHeader);

        Span<byte> infoHeader = stackalloc byte[40];
        BinaryPrimitives.WriteInt32LittleEndian(infoHeader[0..], 40);
        BinaryPrimitives.WriteInt32LittleEndian(infoHeader[4..], width);
        BinaryPrimitives.WriteInt32LittleEndian(infoHeader[8..], -height); // top-down
        BinaryPrimitives.WriteInt16LittleEndian(infoHeader[12..], 1);
        BinaryPrimitives.WriteInt16LittleEndian(infoHeader[14..], 32);
        BinaryPrimitives.WriteInt32LittleEndian(infoHeader[16..], 0); // BI_RGB
        BinaryPrimitives.WriteInt32LittleEndian(infoHeader[20..], pixelBytes);
        output.Write(infoHeader);
        output.Write(bgra, 0, pixelBytes);
    }

    private static void WritePng(Stream output, int width, int height, byte[] bgra)
    {
        static void WriteUInt32BigEndian(Stream s, uint v)
        {
            Span<byte> b = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(b, v);
            s.Write(b);
        }

        static uint Crc32(ReadOnlySpan<byte> data)
        {
            uint crc = 0xFFFFFFFFu;
            for (int i = 0; i < data.Length; i++)
            {
                crc ^= data[i];
                for (int bit = 0; bit < 8; bit++)
                {
                    uint mask = (uint)-(int)(crc & 1);
                    crc = (crc >> 1) ^ (0xEDB88320u & mask);
                }
            }

            return ~crc;
        }

        static uint Adler32(ReadOnlySpan<byte> data)
        {
            const uint mod = 65521;
            uint a = 1;
            uint b = 0;
            for (int i = 0; i < data.Length; i++)
            {
                a = (a + data[i]) % mod;
                b = (b + a) % mod;
            }

            return (b << 16) | a;
        }

        static void WriteChunk(Stream s, ReadOnlySpan<byte> type, ReadOnlySpan<byte> payload)
        {
            WriteUInt32BigEndian(s, (uint)payload.Length);
            s.Write(type);
            s.Write(payload);
            byte[] crcInput = new byte[type.Length + payload.Length];
            type.CopyTo(crcInput);
            payload.CopyTo(crcInput.AsSpan(type.Length));
            WriteUInt32BigEndian(s, Crc32(crcInput));
        }

        byte[] signature = [137, 80, 78, 71, 13, 10, 26, 10];
        output.Write(signature);

        byte[] ihdr = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(0, 4), (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(4, 4), (uint)height);
        ihdr[8] = 8; // bit depth
        ihdr[9] = 6; // RGBA
        ihdr[10] = 0;
        ihdr[11] = 0;
        ihdr[12] = 0;
        WriteChunk(output, "IHDR"u8, ihdr);

        int rowBytes = width * 4;
        byte[] raw = new byte[(rowBytes + 1) * height];
        int src = 0;
        int dst = 0;
        for (int y = 0; y < height; y++)
        {
            raw[dst++] = 0; // filter: none
            for (int x = 0; x < width; x++)
            {
                byte b = bgra[src++];
                byte g = bgra[src++];
                byte r = bgra[src++];
                src++; // ignore source alpha, flatten as opaque for cross-backend text consistency
                raw[dst++] = r;
                raw[dst++] = g;
                raw[dst++] = b;
                raw[dst++] = 255;
            }
        }

        byte[] zlib = BuildZlibNoCompression(raw);
        WriteChunk(output, "IDAT"u8, zlib);
        WriteChunk(output, "IEND"u8, []);

        static byte[] BuildZlibNoCompression(byte[] data)
        {
            using var ms = new MemoryStream();
            ms.WriteByte(0x78); // CMF
            ms.WriteByte(0x01); // FLG

            int offset = 0;
            Span<byte> hdr = stackalloc byte[4];
            while (offset < data.Length)
            {
                int blockLen = Math.Min(65535, data.Length - offset);
                bool final = (offset + blockLen) >= data.Length;
                ms.WriteByte(final ? (byte)0x01 : (byte)0x00);

                ushort len = (ushort)blockLen;
                ushort nlen = (ushort)~len;
                BinaryPrimitives.WriteUInt16LittleEndian(hdr[0..2], len);
                BinaryPrimitives.WriteUInt16LittleEndian(hdr[2..4], nlen);
                ms.Write(hdr);
                ms.Write(data, offset, blockLen);
                offset += blockLen;
            }

            uint adler = Adler32(data);
            Span<byte> ad = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(ad, adler);
            ms.Write(ad);
            return ms.ToArray();
        }
    }

    private static void WriteGif(Stream output, int width, int height, byte[] bgra)
    {
        output.Write("GIF89a"u8);

        Span<byte> lsd = stackalloc byte[7];
        BinaryPrimitives.WriteUInt16LittleEndian(lsd[0..2], (ushort)width);
        BinaryPrimitives.WriteUInt16LittleEndian(lsd[2..4], (ushort)height);
        lsd[4] = 0b1111_0111; // global table present, 8-bit color, size=256
        lsd[5] = 0; // background index
        lsd[6] = 0; // aspect
        output.Write(lsd);

        byte[] palette = new byte[256 * 3];
        for (int i = 0; i < 256; i++)
        {
            int r = (i >> 5) & 0x7;
            int g = (i >> 2) & 0x7;
            int b = i & 0x3;
            palette[(i * 3) + 0] = (byte)((r * 255) / 7);
            palette[(i * 3) + 1] = (byte)((g * 255) / 7);
            palette[(i * 3) + 2] = (byte)((b * 255) / 3);
        }
        output.Write(palette);

        output.WriteByte(0x21);
        output.WriteByte(0xF9);
        output.WriteByte(0x04);
        output.WriteByte(0x00);
        output.WriteByte(0x00);
        output.WriteByte(0x00);
        output.WriteByte(0x00);
        output.WriteByte(0x00);

        output.WriteByte(0x2C);
        Span<byte> id = stackalloc byte[9];
        BinaryPrimitives.WriteUInt16LittleEndian(id[0..2], 0);
        BinaryPrimitives.WriteUInt16LittleEndian(id[2..4], 0);
        BinaryPrimitives.WriteUInt16LittleEndian(id[4..6], (ushort)width);
        BinaryPrimitives.WriteUInt16LittleEndian(id[6..8], (ushort)height);
        id[8] = 0;
        output.Write(id);

        const int minCodeSize = 8;
        output.WriteByte(minCodeSize);

        byte[] indices = new byte[width * height];
        int si = 0;
        for (int i = 0; i < indices.Length; i++)
        {
            byte b = bgra[si++];
            byte g = bgra[si++];
            byte r = bgra[si++];
            si++;
            indices[i] = (byte)((r & 0xE0) | ((g & 0xE0) >> 3) | (b >> 6));
        }

        byte[] lzwData = LzwEncodeGif(indices, minCodeSize);
        int offset = 0;
        while (offset < lzwData.Length)
        {
            int len = Math.Min(255, lzwData.Length - offset);
            output.WriteByte((byte)len);
            output.Write(lzwData, offset, len);
            offset += len;
        }
        output.WriteByte(0x00);

        output.WriteByte(0x3B);
    }

    private static byte[] LzwEncodeGif(byte[] indices, int minCodeSize)
    {
        int clearCode = 1 << minCodeSize;
        int endCode = clearCode + 1;
        int nextCode = endCode + 1;
        int codeSize = minCodeSize + 1;
        int maxCode = (1 << codeSize) - 1;

        var dict = new Dictionary<int, int>(4096);
        var bitWriter = new GifBitWriter(indices.Length);

        void ResetDictionary()
        {
            dict.Clear();
            nextCode = endCode + 1;
            codeSize = minCodeSize + 1;
            maxCode = (1 << codeSize) - 1;
        }

        bitWriter.Write(clearCode, codeSize);
        ResetDictionary();

        int prefix = indices[0];
        for (int i = 1; i < indices.Length; i++)
        {
            int k = indices[i];
            int key = (prefix << 8) | k;
            if (dict.TryGetValue(key, out int combined))
            {
                prefix = combined;
                continue;
            }

            bitWriter.Write(prefix, codeSize);
            if (nextCode < 4096)
            {
                dict[key] = nextCode++;
                if (nextCode > maxCode && codeSize < 12)
                {
                    codeSize++;
                    maxCode = (1 << codeSize) - 1;
                }
            }
            else
            {
                bitWriter.Write(clearCode, codeSize);
                ResetDictionary();
            }

            prefix = k;
        }

        bitWriter.Write(prefix, codeSize);
        bitWriter.Write(endCode, codeSize);
        return bitWriter.ToArray();
    }

    private static void WriteJpeg(Stream output, int width, int height, byte[] bgra, int quality)
    {
        byte[] qY = BuildQuantTable(LumaQuantBase, quality);
        byte[] qC = BuildQuantTable(ChromaQuantBase, quality);

        HuffmanTable dcY = BuildHuffman(DcLumaBits, DcLumaVals);
        HuffmanTable acY = BuildHuffman(AcLumaBits, AcLumaVals);
        HuffmanTable dcC = BuildHuffman(DcChromaBits, DcChromaVals);
        HuffmanTable acC = BuildHuffman(AcChromaBits, AcChromaVals);

        WriteMarker(output, 0xFFD8);
        WriteApp0Jfif(output);
        WriteDqt(output, 0, qY);
        WriteDqt(output, 1, qC);
        WriteSof0(output, width, height);
        WriteDht(output, 0, 0, DcLumaBits, DcLumaVals);
        WriteDht(output, 1, 0, AcLumaBits, AcLumaVals);
        WriteDht(output, 0, 1, DcChromaBits, DcChromaVals);
        WriteDht(output, 1, 1, AcChromaBits, AcChromaVals);
        WriteSos(output);

        var bitWriter = new JpegBitWriter(output);
        var block = new double[64];
        var dct = new double[64];
        var quant = new int[64];

        int prevDcY = 0;
        int prevDcCb = 0;
        int prevDcCr = 0;

        int blocksX = (width + 7) / 8;
        int blocksY = (height + 7) / 8;

        for (int by = 0; by < blocksY; by++)
        {
            for (int bx = 0; bx < blocksX; bx++)
            {
                FillBlockFromBgra(block, bgra, width, height, bx, by, 0);
                ForwardDct(block, dct);
                Quantize(dct, qY, quant);
                EncodeBlock(bitWriter, quant, ref prevDcY, dcY, acY);

                FillBlockFromBgra(block, bgra, width, height, bx, by, 1);
                ForwardDct(block, dct);
                Quantize(dct, qC, quant);
                EncodeBlock(bitWriter, quant, ref prevDcCb, dcC, acC);

                FillBlockFromBgra(block, bgra, width, height, bx, by, 2);
                ForwardDct(block, dct);
                Quantize(dct, qC, quant);
                EncodeBlock(bitWriter, quant, ref prevDcCr, dcC, acC);
            }
        }

        bitWriter.Flush();
        WriteMarker(output, 0xFFD9);
    }

    private static byte[] BuildQuantTable(byte[] baseTable, int quality)
    {
        int q = Math.Clamp(quality, 1, 100);
        int scale = q < 50 ? 5000 / q : 200 - (q * 2);

        byte[] result = new byte[64];
        for (int i = 0; i < 64; i++)
        {
            int value = (baseTable[i] * scale + 50) / 100;
            value = Math.Clamp(value, 1, 255);
            result[i] = (byte)value;
        }

        return result;
    }

    private static void FillBlockFromBgra(double[] block, byte[] bgra, int width, int height, int bx, int by, int component)
    {
        int x0 = bx * 8;
        int y0 = by * 8;

        for (int y = 0; y < 8; y++)
        {
            int sy = Math.Min(height - 1, y0 + y);
            for (int x = 0; x < 8; x++)
            {
                int sx = Math.Min(width - 1, x0 + x);
                int idx = ((sy * width) + sx) * 4;
                double b = bgra[idx + 0];
                double g = bgra[idx + 1];
                double r = bgra[idx + 2];

                double value = component switch
                {
                    0 => (0.299 * r) + (0.587 * g) + (0.114 * b),
                    1 => (-(0.168736 * r)) - (0.331264 * g) + (0.5 * b) + 128.0,
                    _ => (0.5 * r) - (0.418688 * g) - (0.081312 * b) + 128.0,
                };

                block[(y * 8) + x] = Math.Clamp(value, 0.0, 255.0) - 128.0;
            }
        }
    }

    private static void ForwardDct(double[] block, double[] dct)
    {
        const double invSqrt2 = 0.7071067811865475;
        for (int v = 0; v < 8; v++)
        {
            double cv = v == 0 ? invSqrt2 : 1.0;
            for (int u = 0; u < 8; u++)
            {
                double cu = u == 0 ? invSqrt2 : 1.0;
                double sum = 0.0;

                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        sum += block[(y * 8) + x] * Cosine[u, x] * Cosine[v, y];
                    }
                }

                dct[(v * 8) + u] = 0.25 * cu * cv * sum;
            }
        }
    }

    private static void Quantize(double[] dct, byte[] qTable, int[] quant)
    {
        for (int i = 0; i < 64; i++)
        {
            int q = (int)Math.Round(dct[i] / qTable[i]);
            if (i == 0)
            {
                // Baseline JPEG DC category is up to 11 bits.
                q = Math.Clamp(q, -2047, 2047);
            }
            else
            {
                // Baseline JPEG AC category is up to 10 bits.
                q = Math.Clamp(q, -1023, 1023);
            }

            quant[i] = q;
        }
    }

    private static void EncodeBlock(JpegBitWriter writer, int[] quant, ref int prevDc, HuffmanTable dcTable, HuffmanTable acTable)
    {
        int dc = quant[0];
        int diff = dc - prevDc;
        prevDc = dc;

        int dcSize = MagnitudeCategory(diff);
        writer.WriteHuffman(dcTable, dcSize);
        if (dcSize > 0)
        {
            writer.WriteBits(AmplitudeBits(diff, dcSize), dcSize);
        }

        int run = 0;
        for (int i = 1; i < 64; i++)
        {
            int ac = quant[ZigZag[i]];
            if (ac == 0)
            {
                run++;
                continue;
            }

            while (run >= 16)
            {
                writer.WriteHuffman(acTable, 0xF0);
                run -= 16;
            }

            int acSize = MagnitudeCategory(ac);
            int symbol = (run << 4) | acSize;
            writer.WriteHuffman(acTable, symbol);
            writer.WriteBits(AmplitudeBits(ac, acSize), acSize);
            run = 0;
        }

        if (run > 0)
        {
            writer.WriteHuffman(acTable, 0x00);
        }
    }

    private static int MagnitudeCategory(int value)
    {
        if (value == 0) return 0;
        int abs = Math.Abs(value);
        int size = 0;
        while (abs > 0)
        {
            size++;
            abs >>= 1;
        }
        return size;
    }

    private static uint AmplitudeBits(int value, int size)
    {
        if (value >= 0) return (uint)value;
        int mask = (1 << size) - 1;
        return (uint)(value - 1) & (uint)mask;
    }

    private static HuffmanTable BuildHuffman(byte[] bits, byte[] vals)
    {
        var table = new HuffmanTable();
        int code = 0;
        int k = 0;

        for (int len = 1; len <= 16; len++)
        {
            int count = bits[len];
            for (int i = 0; i < count; i++)
            {
                int symbol = vals[k++];
                table.Codes[symbol] = (ushort)code;
                table.Sizes[symbol] = (byte)len;
                code++;
            }

            code <<= 1;
        }

        return table;
    }

    private static void WriteMarker(Stream s, ushort marker)
    {
        Span<byte> b = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(b, marker);
        s.Write(b);
    }

    private static void WriteSegment(Stream s, ushort marker, ReadOnlySpan<byte> payload)
    {
        WriteMarker(s, marker);
        Span<byte> len = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(len, (ushort)(payload.Length + 2));
        s.Write(len);
        s.Write(payload);
    }

    private static void WriteApp0Jfif(Stream s)
    {
        byte[] payload =
        [
            (byte)'J', (byte)'F', (byte)'I', (byte)'F', 0,
            1, 1,
            0,
            0, 1,
            0, 1,
            0, 0,
        ];
        WriteSegment(s, 0xFFE0, payload);
    }

    private static void WriteDqt(Stream s, int tableId, byte[] table)
    {
        byte[] payload = new byte[65];
        payload[0] = (byte)tableId;
        for (int i = 0; i < 64; i++)
        {
            payload[i + 1] = table[ZigZag[i]];
        }

        WriteSegment(s, 0xFFDB, payload);
    }

    private static void WriteSof0(Stream s, int width, int height)
    {
        byte[] payload = new byte[17];
        payload[0] = 8;
        BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(1, 2), (ushort)height);
        BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(3, 2), (ushort)width);
        payload[5] = 3;

        payload[6] = 1;
        payload[7] = 0x11;
        payload[8] = 0;

        payload[9] = 2;
        payload[10] = 0x11;
        payload[11] = 1;

        payload[12] = 3;
        payload[13] = 0x11;
        payload[14] = 1;

        WriteSegment(s, 0xFFC0, payload);
    }

    private static void WriteDht(Stream s, int tableClass, int tableId, byte[] bits, byte[] vals)
    {
        byte[] payload = new byte[1 + 16 + vals.Length];
        payload[0] = (byte)((tableClass << 4) | tableId);
        for (int i = 0; i < 16; i++)
        {
            payload[1 + i] = bits[i + 1];
        }

        vals.CopyTo(payload, 17);
        WriteSegment(s, 0xFFC4, payload);
    }

    private static void WriteSos(Stream s)
    {
        byte[] payload =
        [
            3,
            1, 0x00,
            2, 0x11,
            3, 0x11,
            0,
            63,
            0,
        ];

        WriteSegment(s, 0xFFDA, payload);
    }

    private static double[,] BuildCosineTable()
    {
        var t = new double[8, 8];
        for (int u = 0; u < 8; u++)
        {
            for (int x = 0; x < 8; x++)
            {
                t[u, x] = Math.Cos(((2 * x) + 1) * u * Math.PI / 16.0);
            }
        }

        return t;
    }

    private sealed class HuffmanTable
    {
        public readonly ushort[] Codes = new ushort[256];
        public readonly byte[] Sizes = new byte[256];
    }

    private sealed class JpegBitWriter(Stream output)
    {
        private readonly Stream _output = output;
        private uint _buffer;
        private int _bits;

        public void WriteHuffman(HuffmanTable table, int symbol)
        {
            WriteBits(table.Codes[symbol], table.Sizes[symbol]);
        }

        public void WriteBits(uint value, int count)
        {
            if (count <= 0) return;

            _buffer = (_buffer << count) | (value & ((1u << count) - 1));
            _bits += count;

            while (_bits >= 8)
            {
                _bits -= 8;
                byte b = (byte)((_buffer >> _bits) & 0xFF);
                _output.WriteByte(b);
                if (b == 0xFF)
                {
                    _output.WriteByte(0x00);
                }
            }
        }

        public void Flush()
        {
            if (_bits > 0)
            {
                uint pad = (uint)((1 << (8 - _bits)) - 1);
                WriteBits(pad, 8 - _bits);
            }
        }
    }

    private sealed class GifBitWriter(int capacityHint)
    {
        private readonly List<byte> _bytes = new(Math.Max(64, capacityHint / 2));
        private int _bitBuffer;
        private int _bitCount;

        public void Write(int code, int bits)
        {
            _bitBuffer |= (code << _bitCount);
            _bitCount += bits;
            while (_bitCount >= 8)
            {
                _bytes.Add((byte)(_bitBuffer & 0xFF));
                _bitBuffer >>= 8;
                _bitCount -= 8;
            }
        }

        public byte[] ToArray()
        {
            if (_bitCount > 0)
            {
                _bytes.Add((byte)(_bitBuffer & 0xFF));
                _bitBuffer = 0;
                _bitCount = 0;
            }

            return _bytes.ToArray();
        }
    }
}
