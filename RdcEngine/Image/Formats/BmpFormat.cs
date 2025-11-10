using RdcEngine.Exceptions;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using static RdcEngine.Image.ColorModel;

namespace RdcEngine.Image.Formats;

internal static class BmpFormat
{
    private const ushort InfoHeaderSize = 14;

    private const ushort DibHeaderSize = 40;

    private const ushort BaseHeaderSize = InfoHeaderSize + DibHeaderSize;

    private const ushort Signature = 0x4D42;

    private const ushort Alignment = 4;

    private const int MaxImageLength = 1 << 14;

    private const uint MaxPayloadSize = 1u << 30;

    [SkipLocalsInit]
    public static RawImage Load(Stream bmpInput)
    {
        StreamValidator.EnsureReadable(bmpInput);
        StreamValidator.EnsureRemaining(bmpInput, BaseHeaderSize);

        Span<byte> header = stackalloc byte[BaseHeaderSize];
        bmpInput.ReadExactly(header);

        ushort signature = BinaryPrimitives.ReadUInt16LittleEndian(header[00..02]);
        uint   fileSize  = BinaryPrimitives.ReadUInt32LittleEndian(header[02..06]);
        uint   offBits   = BinaryPrimitives.ReadUInt32LittleEndian(header[10..14]);

        uint   dibSize   = BinaryPrimitives.ReadUInt32LittleEndian(header[14..18]);
        int    width     = BinaryPrimitives.ReadInt32LittleEndian (header[18..22]);
        int    height    = BinaryPrimitives.ReadInt32LittleEndian (header[22..26]);
        ushort planes    = BinaryPrimitives.ReadUInt16LittleEndian(header[26..28]);
        ushort bpp       = BinaryPrimitives.ReadUInt16LittleEndian(header[28..30]);
        uint   compress  = BinaryPrimitives.ReadUInt32LittleEndian(header[30..34]);
        uint   imageSize = BinaryPrimitives.ReadUInt32LittleEndian(header[34..38]);
        uint   palette   = BinaryPrimitives.ReadUInt32LittleEndian(header[46..50]);

        if (signature is not Signature)
            throw new MalformedFileException("Missing BMP signature");

        if (dibSize is < DibHeaderSize)
            throw new VariantNotSupportedException("Unsupported DIB header size for BMP");

        if (/* offBits > fileSize || */ offBits < dibSize + InfoHeaderSize)
            throw new MalformedFileException("Invalid pixel data offset in BMP header");

        if (width is <= 0 || height is 0)
            throw new MalformedFileException("Invalid BMP dimensions");

        if (planes is not 1)
            throw new VariantNotSupportedException("Unsupported number of planes for BMP");

        if (bpp is not (Gray * 8 or Rgb * 8 or Rgba * 8))
            throw new VariantNotSupportedException("Only Gray, RGB, and RGBA color models are supported for BMP");

        if (compress is not 0)
            throw new VariantNotSupportedException("Compressed BMP is not supported");

        if (width is > MaxImageLength)
            throw new ConstraintViolationException($"Width of BMP must not exceed {MaxImageLength}");

        if (height is > MaxImageLength)
            throw new ConstraintViolationException($"Height of BMP must not exceed {MaxImageLength}");

        long size = bmpInput.Length - offBits;
        int rows = height < 0 ? -height : height;

        if (size > MaxPayloadSize)
            throw new ConstraintViolationException("BMP image too big to load");

        uint bytesPerPixel = bpp / 8u;
        long stride = (width * bytesPerPixel + Alignment - 1) & ~(Alignment - 1);
        long pixelBytes = stride * rows;
        int expectedOffBits = BaseHeaderSize;

        if (bpp is 8)
        {
            int paletteEntries = (int)((offBits - BaseHeaderSize) / 4);
            expectedOffBits += 256 * 4;

            if (palette is not (0 or 256) || paletteEntries is not 256)
                throw new VariantNotSupportedException("Palette size for grayscale BMP must be 256");

            if (offBits < expectedOffBits)
                throw new MalformedFileException("Invalid pixel data offset for grayscale BMP");

            Span<byte> entries = stackalloc byte[256 * 4];
            bmpInput.ReadExactly(entries);

            for (int i = 0; i < 256; i++)
            {
                byte b = entries[i * 4 + 0];
                byte g = entries[i * 4 + 1];
                byte r = entries[i * 4 + 2];
                byte a = entries[i * 4 + 3];

                if (r != i || g != i || b != i || a != 0)
                    throw new VariantNotSupportedException("Only palettes with a grayscale ramp is supported for BMP");
            }

            bmpInput.Seek(offBits, SeekOrigin.Begin);
        }
        else if (offBits is < BaseHeaderSize)
        {
            throw new MalformedFileException("Unexpected pixel data offset for true-color BMP");
        }

        bmpInput.Seek(offBits, SeekOrigin.Begin);

        long expectedEnd = offBits + pixelBytes;

        if (imageSize is not 0 && imageSize != pixelBytes)
            throw new MalformedFileException("BMP header contains incorrect image size");

        if (expectedEnd > bmpInput.Length)
            throw new MalformedFileException("BMP file has incomplete pixel data");

        StreamValidator.EnsureRemaining(bmpInput, (ulong)(expectedEnd - expectedOffBits));

        byte[] raw = GC.AllocateUninitializedArray<byte>((int)pixelBytes);

        if (height < 0)
        {
            bmpInput.ReadExactly(raw);
        }
        else
        {
            for (int i = (int)(pixelBytes - stride); i >= 0; i -= (int)stride)
            {
                bmpInput.ReadExactly(raw.AsSpan(i, (int)stride));
            }
        }

        return new(width, rows, (int)stride, (int)bytesPerPixel, (int)pixelBytes, raw);
    }

    [SkipLocalsInit]
    public static void Save(RawImage rawImage, Stream bmpOutput)
    {
        StreamValidator.EnsureWritable(bmpOutput);
        ArgumentNullException.ThrowIfNull(rawImage.Data);

        var (width, height, strideInput, colorModel, size, data) = rawImage;

        if (width is <= 0 || height is <= 0)
            throw new MalformedDataException("Width and height of BMP must be positive");

        if (width is > MaxImageLength)
            throw new ConstraintViolationException($"Width of BMP must not exceed {MaxImageLength}");

        if (height is > MaxImageLength)
            throw new ConstraintViolationException($"Height of BMP must not exceed {MaxImageLength}");

        if (colorModel is not (Gray or Rgb or Rgba))
            throw new VariantNotSupportedException("Only Gray, RGB, and RGBA color models are supported for BMP");

        if (strideInput < width * colorModel)
            throw new MalformedDataException("Stride of BMP is too small for its width");

        int stride = (width * colorModel + Alignment - 1) & ~(Alignment - 1);

        if (data.Length < size)
            throw new MalformedDataException("BMP file has incomplete pixel data");

        uint paletteEntries = colorModel is Gray ? 256u : 0;
        uint paletteSize = paletteEntries * 4;
        uint offBits = BaseHeaderSize + paletteSize;
        int fileSize = (int)offBits + size;

        Span<byte> header = stackalloc byte[BaseHeaderSize];

        BinaryPrimitives.WriteUInt16LittleEndian(header[00..02], Signature);
        BinaryPrimitives.WriteUInt32LittleEndian(header[02..06], offBits + (uint)size);
        BinaryPrimitives.WriteUInt16LittleEndian(header[06..08], 0);
        BinaryPrimitives.WriteUInt16LittleEndian(header[08..10], 0);
        BinaryPrimitives.WriteUInt32LittleEndian(header[10..14], offBits);

        BinaryPrimitives.WriteUInt32LittleEndian(header[14..18], DibHeaderSize);
        BinaryPrimitives.WriteInt32LittleEndian (header[18..22], width);
        BinaryPrimitives.WriteInt32LittleEndian (header[22..26], -height);
        BinaryPrimitives.WriteUInt16LittleEndian(header[26..28], 1);
        BinaryPrimitives.WriteUInt16LittleEndian(header[28..30], (ushort)(colorModel * 8));
        BinaryPrimitives.WriteUInt32LittleEndian(header[30..34], 0);
        BinaryPrimitives.WriteUInt32LittleEndian(header[34..38], (uint)size);
        BinaryPrimitives.WriteInt32LittleEndian (header[38..42], 0);
        BinaryPrimitives.WriteInt32LittleEndian (header[42..46], 0);
        BinaryPrimitives.WriteUInt32LittleEndian(header[46..50], paletteEntries);
        BinaryPrimitives.WriteUInt32LittleEndian(header[50..54], 0);

        bmpOutput.Write(header);

        if (colorModel is Gray)
        {
            for (int i = 0; i < 256; ++i)
            {
                bmpOutput.WriteByte((byte)i);
                bmpOutput.WriteByte((byte)i);
                bmpOutput.WriteByte((byte)i);
                bmpOutput.WriteByte(0);
            }
        }

        for (int i = 0, row = width * colorModel, pad = stride - row; i < size; i += row)
        {
            bmpOutput.Write(data, i, row);

            for (uint j = 0; j < pad; ++j)
                bmpOutput.WriteByte(0);
        }
    }
}
