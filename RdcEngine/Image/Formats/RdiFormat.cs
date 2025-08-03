using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;

namespace RdcEngine.Image.Formats;

internal static class RdiFormat
{
    private const ushort HeaderSize = 28;

    private const ulong Signature = 0x00494452_00524E41;

    private const ushort Offset = 32;

    [SkipLocalsInit]
    public static RawImage Load(Stream rdiInput, out ushort mode)
    {
        StreamValidator.EnsureReadable(rdiInput);
        StreamValidator.EnsureRemaining(rdiInput, HeaderSize);

        Span<byte> header = stackalloc byte[HeaderSize];
        rdiInput.ReadExactly(header);

        ulong  signature  = BinaryPrimitives.ReadUInt64LittleEndian(header[00..08]);
        ushort version    = BinaryPrimitives.ReadUInt16LittleEndian(header[08..10]);
        uint   dataOffset = BinaryPrimitives.ReadUInt32LittleEndian(header[10..14]);
        uint   width      = BinaryPrimitives.ReadUInt32LittleEndian(header[14..18]);
        uint   height     = BinaryPrimitives.ReadUInt32LittleEndian(header[18..22]);
        ushort colorSpace = BinaryPrimitives.ReadUInt16LittleEndian(header[22..24]);
        ushort colorDepth = BinaryPrimitives.ReadUInt16LittleEndian(header[24..26]);
               mode       = BinaryPrimitives.ReadUInt16LittleEndian(header[26..28]);

        if (signature is not Signature)
            throw new InvalidDataException("Missing RDI signature");

        if (version is not 1)
            throw new NotSupportedException("Unsupported RDI version");

        if (dataOffset < HeaderSize)
            throw new InvalidDataException("RDI data offset less than header size");

        if (width is 0 || height is 0)
            throw new InvalidDataException("Width and height of RDI must be positive");

        if (colorSpace is not (3 or 4))
            throw new NotSupportedException("Unsupported color space for RDI");

        if (colorDepth is not 8)
            throw new NotSupportedException("Unsupported color depth for RDI");

        const uint hardLimit = 1 << 17;
        const long maxSize = (1L << 31) - 1;
        long size = rdiInput.Length - dataOffset;

        if (width is > hardLimit)
            throw new InvalidDataException($"Width of RDI must not exceed {hardLimit}");

        if (height is > hardLimit)
            throw new InvalidDataException($"Height of RDI must not exceed {hardLimit}");

        if (size > maxSize)
            throw new InvalidDataException("RDI image too big to load");

        StreamValidator.EnsureRemaining(rdiInput, dataOffset - HeaderSize + 1u);
        rdiInput.Seek(dataOffset, SeekOrigin.Begin);

        byte[] raw = GC.AllocateUninitializedArray<byte>((int)size);
        rdiInput.ReadExactly(raw);

        return new((int)width, (int)height, (int)width * colorSpace * (colorDepth / 8), colorSpace, (int)size, raw);
    }

    [SkipLocalsInit]
    public static void Save(RawImage rawImage, Stream rdiOutput, ushort mode)
    {
        StreamValidator.EnsureWritable(rdiOutput);
        ArgumentNullException.ThrowIfNull(rawImage.Data);

        var (width, height, _, colorSpace, size, data) = rawImage;

        if (width is <= 0 || height is <= 0)
            throw new ArgumentException("Width and height of RDI must be positive", nameof(rawImage));

        const int hardLimit = 1 << 17;

        if (width is > hardLimit)
            throw new ArgumentException($"Width of RDI must not exceed {hardLimit}", nameof(rawImage));

        if (height is > hardLimit)
            throw new ArgumentException($"Height of RDI must not exceed {hardLimit}", nameof(rawImage));

        if (colorSpace is not (3 or 4))
            throw new ArgumentException("Unsupported color space for RDI", nameof(rawImage));

        Span<byte> header = stackalloc byte[HeaderSize];
        BinaryPrimitives.WriteUInt64LittleEndian(header[00..08], Signature);
        BinaryPrimitives.WriteUInt16LittleEndian(header[08..10], 1);
        BinaryPrimitives.WriteUInt32LittleEndian(header[10..14], Offset);
        BinaryPrimitives.WriteUInt32LittleEndian(header[14..18], (uint)width);
        BinaryPrimitives.WriteUInt32LittleEndian(header[18..22], (uint)height);
        BinaryPrimitives.WriteUInt16LittleEndian(header[22..24], (ushort)colorSpace);
        BinaryPrimitives.WriteUInt16LittleEndian(header[24..26], 8);
        BinaryPrimitives.WriteUInt16LittleEndian(header[26..28], mode);

        rdiOutput.Write(header);

        for (int i = 0; i < Offset - HeaderSize; ++i)
            rdiOutput.WriteByte(0);

        rdiOutput.Write(data, 0, size);
    }
}
