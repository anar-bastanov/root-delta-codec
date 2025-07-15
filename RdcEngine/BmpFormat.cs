using System;
using System.Buffers.Binary;
using System.IO;

namespace RdcEngine;

internal static class BmpFormat
{
    public static RawImage Load(Stream bmpInput)
    {
        StreamValidator.EnsureReadable(bmpInput);
        StreamValidator.EnsureRemaining(bmpInput, 54);

        Span<byte> hdr = stackalloc byte[54];
        bmpInput.ReadExactly(hdr);

        ushort signature = BinaryPrimitives.ReadUInt16LittleEndian(hdr[..2]);
        uint fileSize = BinaryPrimitives.ReadUInt32LittleEndian(hdr[2..6]);
        uint offBits = BinaryPrimitives.ReadUInt32LittleEndian(hdr[10..14]);

        uint dibSize = BinaryPrimitives.ReadUInt32LittleEndian(hdr[14..18]);
        int width = BinaryPrimitives.ReadInt32LittleEndian(hdr[18..22]);
        int height = BinaryPrimitives.ReadInt32LittleEndian(hdr[22..26]);
        ushort planes = BinaryPrimitives.ReadUInt16LittleEndian(hdr[26..28]);
        ushort bpp = BinaryPrimitives.ReadUInt16LittleEndian(hdr[28..30]);
        uint compress = BinaryPrimitives.ReadUInt32LittleEndian(hdr[30..34]);
        uint imageSize = BinaryPrimitives.ReadUInt32LittleEndian(hdr[34..38]);

        if (signature is not 0x4D42)
            throw new InvalidDataException("Missing BMP signature");

        // if (offBits is not 54 || dibSize is not 40)
        //     throw new NotSupportedException("Only BITMAPINFOHEADER with 54-byte header is supported");

        if (planes is not 1)
            throw new NotSupportedException("Invalid number of planes");

        if (bpp is not 24 or 32)
            throw new NotSupportedException("Only 24-bit and 32-bit BMPs are supported");

        if (compress is not 0)
            throw new NotSupportedException("Compressed BMPs are not supported");

        if (width is <= 0 || height is 0)
            throw new InvalidDataException("Invalid BMP dimensions");

        int bytesPerPixel = bpp / 8;
        int stride = (width * bytesPerPixel + 3) & ~3;
        int rows = Math.Abs(height);
        int pixelBytes = stride * rows;

        if (imageSize is not 0 && imageSize != pixelBytes)
            throw new InvalidDataException("BMP header contains incorrect image size");

        long pixelDataStart = offBits;
        long expectedEnd = pixelDataStart + pixelBytes;

        if (expectedEnd > bmpInput.Length)
            throw new InvalidDataException("BMP pixel data is incomplete or corrupted");

        if (expectedEnd != bmpInput.Length)
            throw new InvalidDataException("BMP size mismatch");

        if (offBits is > 54)
            bmpInput.Seek(offBits - 54, SeekOrigin.Current);

        StreamValidator.EnsureRemaining(bmpInput, pixelBytes);

        byte[] raw = new byte[pixelBytes];
        bmpInput.ReadExactly(raw);

        return new(width, height, stride, bytesPerPixel, raw);
    }

    public static void Save(RawImage rawImage, Stream bmpOutput)
    {
        StreamValidator.EnsureWritable(bmpOutput);
        ArgumentNullException.ThrowIfNull(rawImage.Data);

        var (width, height, stride, channels, data) = rawImage;

        if (width is <= 0)
            throw new ArgumentException("Width must be positive", nameof(rawImage));

        if (height is 0)
            throw new ArgumentException("Height cannot be zero", nameof(rawImage));

        if (channels is not 3 and not 4)
            throw new ArgumentException("Only 24-bit and 32-bit BMPs are supported", nameof(rawImage));

        if (stride != ((width * channels + 3) & ~3))
            throw new ArgumentException("Stride does not match width and channels", nameof(rawImage));

        int absHeight = Math.Abs(height);
        int pixelDataSize = stride * absHeight;

        if (data.Length != pixelDataSize)
            throw new ArgumentException("Data length does not match expected pixel data size", nameof(rawImage));

        Span<byte> header = stackalloc byte[54];

        BinaryPrimitives.WriteUInt16LittleEndian(header[0..2], 0x4D42);
        BinaryPrimitives.WriteUInt32LittleEndian(header[2..6], (uint)(14 + 40 + pixelDataSize));
        BinaryPrimitives.WriteUInt16LittleEndian(header[6..8], 0);
        BinaryPrimitives.WriteUInt16LittleEndian(header[8..10], 0);
        BinaryPrimitives.WriteUInt32LittleEndian(header[10..14], 54);

        BinaryPrimitives.WriteUInt32LittleEndian(header[14..18], 40);
        BinaryPrimitives.WriteInt32LittleEndian(header[18..22], width);
        BinaryPrimitives.WriteInt32LittleEndian(header[22..26], height);
        BinaryPrimitives.WriteUInt16LittleEndian(header[26..28], 1);
        BinaryPrimitives.WriteUInt16LittleEndian(header[28..30], (ushort)(channels * 8));
        BinaryPrimitives.WriteUInt32LittleEndian(header[30..34], 0);
        BinaryPrimitives.WriteUInt32LittleEndian(header[34..38], (uint)pixelDataSize);
        BinaryPrimitives.WriteInt32LittleEndian(header[38..42], 0);
        BinaryPrimitives.WriteInt32LittleEndian(header[42..46], 0);
        BinaryPrimitives.WriteUInt32LittleEndian(header[46..50], 0);
        BinaryPrimitives.WriteUInt32LittleEndian(header[50..54], 0);

        bmpOutput.Write(header);
        bmpOutput.Write(data);
    }
}
