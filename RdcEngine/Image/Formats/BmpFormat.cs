﻿using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;

namespace RdcEngine.Image.Formats;

internal static class BmpFormat
{
    private const ushort InfoHeaderSize = 14;

    private const ushort DibHeaderSize = 40;

    private const ushort HeaderSize = InfoHeaderSize + DibHeaderSize;

    private const ushort Signature = 0x4D42;

    private const ushort Alignment = 4;

    [SkipLocalsInit]
    public static RawImage Load(Stream bmpInput)
    {
        StreamValidator.EnsureReadable(bmpInput);
        StreamValidator.EnsureRemaining(bmpInput, HeaderSize);

        Span<byte> hdr = stackalloc byte[HeaderSize];
        bmpInput.ReadExactly(hdr);

        ushort signature = BinaryPrimitives.ReadUInt16LittleEndian(hdr[00..02]);
        uint fileSize = BinaryPrimitives.ReadUInt32LittleEndian(hdr[02..06]);
        uint offBits = BinaryPrimitives.ReadUInt32LittleEndian(hdr[10..14]);

        uint dibSize = BinaryPrimitives.ReadUInt32LittleEndian(hdr[14..18]);
        int width = BinaryPrimitives.ReadInt32LittleEndian(hdr[18..22]);
        int height = BinaryPrimitives.ReadInt32LittleEndian(hdr[22..26]);
        ushort planes = BinaryPrimitives.ReadUInt16LittleEndian(hdr[26..28]);
        ushort bpp = BinaryPrimitives.ReadUInt16LittleEndian(hdr[28..30]);
        uint compress = BinaryPrimitives.ReadUInt32LittleEndian(hdr[30..34]);
        uint imageSize = BinaryPrimitives.ReadUInt32LittleEndian(hdr[34..38]);

        if (signature is not Signature)
            throw new InvalidDataException("Missing BMP signature");

        if (dibSize is < DibHeaderSize)
            throw new NotSupportedException("Unsupported DIB header size for BMP");

        if (offBits < HeaderSize || offBits > fileSize || offBits < dibSize + InfoHeaderSize)
            throw new InvalidDataException("Invalid pixel data offset in BMP header");

        if (planes is not 1)
            throw new NotSupportedException("Unsupported number of planes for BMP");

        if (bpp is not 24 and not 32)
            throw new NotSupportedException("Only 24-bit and 32-bit BMPs are supported");

        if (compress is not 0)
            throw new NotSupportedException("Compressed BMPs are not supported");

        if (width is <= 0 || height is 0)
            throw new InvalidDataException("Invalid BMP dimensions");

        uint bytesPerPixel = bpp / 8u;
        uint stride = ((uint)width * bytesPerPixel + Alignment - 1) & ~(uint)(Alignment - 1);
        uint rows = (uint)(height < 0 ? -height : height);
        uint pixelBytes = checked(stride * rows);

        if (imageSize != 0 && imageSize != pixelBytes)
            throw new InvalidDataException("BMP header contains incorrect image size");

        ulong expectedEnd = offBits + pixelBytes;
        if (expectedEnd != (ulong)bmpInput.Length)
            throw new InvalidDataException("BMP size mismatch or incomplete pixel data");

        StreamValidator.EnsureRemaining(bmpInput, expectedEnd - HeaderSize);
        bmpInput.Seek(offBits - HeaderSize, SeekOrigin.Current);

        byte[] raw = GC.AllocateUninitializedArray<byte>((int)pixelBytes);

        if (height < 0)
        {
            bmpInput.ReadExactly(raw);
        }
        else
        {
            for (uint i = pixelBytes - stride; (int)i >= 0; i -= stride)
            {
                bmpInput.ReadExactly(raw.AsSpan((int)i, (int)stride));
            }
        }

        return new((uint)width, rows, stride, bytesPerPixel, pixelBytes, raw);
    }

    [SkipLocalsInit]
    public static void Save(RawImage rawImage, Stream bmpOutput)
    {
        StreamValidator.EnsureWritable(bmpOutput);
        ArgumentNullException.ThrowIfNull(rawImage.Data);

        var (width, height, strideInput, channels, size, data) = rawImage;

        if (width is 0 || height is 0)
            throw new ArgumentException("Width and height of BMP must be non-zero", nameof(rawImage));

        if (channels is not 3 and not 4)
            throw new ArgumentException("Only 24-bit and 32-bit BMP is supported", nameof(rawImage));

        if (strideInput < width * channels)
            throw new ArgumentException("Stride of BMP is too small for its width", nameof(rawImage));

        uint stride = (width * channels + Alignment - 1) & ~(uint)(Alignment - 1);

        if (data.Length < size)
            throw new ArgumentException("BMP size mismatch or incomplete pixel data", nameof(rawImage));

        Span<byte> header = stackalloc byte[HeaderSize];

        BinaryPrimitives.WriteUInt16LittleEndian(header[00..02], Signature);
        BinaryPrimitives.WriteUInt32LittleEndian(header[02..06], HeaderSize + size);
        BinaryPrimitives.WriteUInt16LittleEndian(header[06..08], 0);
        BinaryPrimitives.WriteUInt16LittleEndian(header[08..10], 0);
        BinaryPrimitives.WriteUInt32LittleEndian(header[10..14], HeaderSize);

        BinaryPrimitives.WriteUInt32LittleEndian(header[14..18], DibHeaderSize);
        BinaryPrimitives.WriteUInt32LittleEndian(header[18..22], width);
        BinaryPrimitives.WriteInt32LittleEndian(header[22..26], -checked((int)height));
        BinaryPrimitives.WriteUInt16LittleEndian(header[26..28], 1);
        BinaryPrimitives.WriteUInt16LittleEndian(header[28..30], (ushort)(channels * 8));
        BinaryPrimitives.WriteUInt32LittleEndian(header[30..34], 0);
        BinaryPrimitives.WriteUInt32LittleEndian(header[34..38], size);
        BinaryPrimitives.WriteInt32LittleEndian(header[38..42], 0);
        BinaryPrimitives.WriteInt32LittleEndian(header[42..46], 0);
        BinaryPrimitives.WriteUInt32LittleEndian(header[46..50], 0);
        BinaryPrimitives.WriteUInt32LittleEndian(header[50..54], 0);

        bmpOutput.Write(header);

        for (uint i = 0, row = width * channels, pad = stride - row; i < size; i += stride)
        {
            bmpOutput.Write(data, (int)i, (int)row);

            for (uint j = 0; j < pad; ++j)
                bmpOutput.WriteByte(0);
        }
    }

    [Obsolete]
    private static void FlipVertically(byte[] data, int stride, int rows)
    {
        for (int top = 0, bot = rows * stride; top < bot;)
        {
            bot -= stride;
            Span<byte> a = data.AsSpan(top, stride);
            Span<byte> b = data.AsSpan(bot, stride);
            top += stride;

            for (int i = 0; i < stride; ++i)
                (a[i], b[i]) = (b[i], a[i]);
        }
    }
}
