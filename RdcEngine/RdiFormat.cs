using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;

namespace RdcEngine;

internal static class RdiFormat
{
    private const ushort HeaderSize = 26;

    private const ulong Signature = 0x00494452_00524E41;

    private const ushort Version = 0x0100;

    private const ushort Mode = 0x0001;

    private const ushort Alignment = 1;

    private const ushort Offset = 6;

    [SkipLocalsInit]
    public static RawImage Load(Stream rdiInput)
    {
        StreamValidator.EnsureReadable(rdiInput);
        StreamValidator.EnsureRemaining(rdiInput, HeaderSize);

        Span<byte> header = stackalloc byte[HeaderSize];
        rdiInput.ReadExactly(header);

        ulong signature = BinaryPrimitives.ReadUInt64LittleEndian(header[0..8]);
        ushort version = BinaryPrimitives.ReadUInt16LittleEndian(header[8..10]);

        ushort mode = BinaryPrimitives.ReadUInt16LittleEndian(header[10..12]);
        ushort channels = BinaryPrimitives.ReadUInt16LittleEndian(header[12..14]);
        uint width = BinaryPrimitives.ReadUInt32LittleEndian(header[14..18]);
        uint height = BinaryPrimitives.ReadUInt32LittleEndian(header[18..22]);
        ushort alignment = BinaryPrimitives.ReadUInt16LittleEndian(header[22..24]);
        ushort offset = BinaryPrimitives.ReadUInt16LittleEndian(header[24..26]);

        if (signature is not Signature)
            throw new InvalidDataException("Invalid RDI signature");

        if (version is not Version)
            throw new InvalidDataException("Unsupported RDI version");

        if (mode is not Mode)
            throw new InvalidDataException("Unsupported encoding mode");

        if (channels is not 3 and not 4)
            throw new NotSupportedException("Only 24-bit and 32-bit RDIs are supported");

        if (alignment is 0)
            throw new InvalidDataException("Invalid alignment");

        if (width is 0 || height is 0)
            throw new InvalidDataException("Invalid RDI dimensions");

        uint stride = (width * channels + alignment - 1) & ~(uint)(alignment - 1);
        uint pixelBytes = checked(stride * height);

        ulong expectedEnd = (ulong)HeaderSize + offset + pixelBytes;
        if (expectedEnd != (ulong)rdiInput.Length)
            throw new InvalidDataException("rdi size mismatch or pixel raw is incomplete");

        StreamValidator.EnsureRemaining(rdiInput, expectedEnd - HeaderSize - offset);
        rdiInput.Seek(offset, SeekOrigin.Current);

        byte[] raw = GC.AllocateUninitializedArray<byte>((int)pixelBytes);
        rdiInput.ReadExactly(raw);

        return new RawImage(width, height, stride, channels, raw);
    }

    [SkipLocalsInit]
    public static void Save(RawImage rawImage, Stream rdiOutput)
    {
        StreamValidator.EnsureWritable(rdiOutput);
        ArgumentNullException.ThrowIfNull(rawImage.Data);

        var (width, height, strideInput, channels, data) = rawImage;

        if (width is 0 || height is 0)
            throw new ArgumentException("Width and height must be non-zero", nameof(rawImage));

        if (channels is not 3 and not 4)
            throw new ArgumentException("Invalid channel count for RDI", nameof(rawImage));

        if (strideInput < width * channels)
            throw new ArgumentException("Stride is too small for width", nameof(rawImage));

        uint stride = (width * channels + Alignment - 1) & ~(uint)(Alignment - 1);
        uint pixelBytes = checked(stride * height);

        if (data.Length != pixelBytes)
            throw new ArgumentException("Data length mismatch", nameof(rawImage));

        Span<byte> header = stackalloc byte[HeaderSize + Offset];
        BinaryPrimitives.WriteUInt64LittleEndian(header[0..8], Signature);
        BinaryPrimitives.WriteUInt16LittleEndian(header[8..10], Version);
        BinaryPrimitives.WriteUInt16LittleEndian(header[10..12], Mode);
        BinaryPrimitives.WriteUInt16LittleEndian(header[12..14], (ushort)channels);
        BinaryPrimitives.WriteUInt32LittleEndian(header[14..18], width);
        BinaryPrimitives.WriteUInt32LittleEndian(header[18..22], height);
        BinaryPrimitives.WriteUInt16LittleEndian(header[22..24], Alignment);
        BinaryPrimitives.WriteUInt16LittleEndian(header[24..26], Offset);
        BinaryPrimitives.WriteUInt32LittleEndian(header[26..30], 0);
        BinaryPrimitives.WriteUInt16LittleEndian(header[30..32], 0);

        rdiOutput.Write(header);

        for (uint i = 0, size = width * channels, pad = stride - size; i < pixelBytes; i += stride)
        {
            rdiOutput.Write(data, (int)i, (int)size);

            for (uint j = 0; j < pad; ++j)
                rdiOutput.WriteByte(0);
        }
    }
}
