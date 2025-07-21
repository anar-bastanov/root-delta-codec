using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;

namespace RdcEngine;

internal static class RdiFormat
{
    private const ushort HeaderSize = 24;

    private const ulong Signature = 0x00494452_00524E41;

    private const ushort Version = 0x01_00;

    private const ushort Mode = 0x0001;

    private const ushort Offset = 8;

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
        ushort offset = BinaryPrimitives.ReadUInt16LittleEndian(header[22..24]);

        if (signature is not Signature)
            throw new InvalidDataException("Invalid RDI signature");

        if (version is not Version)
            throw new InvalidDataException("Unsupported RDI version");

        if (mode is not Mode)
            throw new InvalidDataException("Unsupported encoding mode");

        if (channels is not 3 and not 4)
            throw new NotSupportedException("Only 24-bit and 32-bit RDIs are supported");

        if (width is 0 || height is 0)
            throw new InvalidDataException("Invalid RDI dimensions");

        StreamValidator.EnsureRemaining(rdiInput, offset + 1u);
        rdiInput.Seek(offset, SeekOrigin.Current);

        byte[] raw = GC.AllocateUninitializedArray<byte>(checked((int)(rdiInput.Length - rdiInput.Position)));
        rdiInput.ReadExactly(raw);

        return new RawImage(width, height, checked(width * channels), channels, raw);
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

        Span<byte> header = stackalloc byte[HeaderSize];
        BinaryPrimitives.WriteUInt64LittleEndian(header[0..8], Signature);
        BinaryPrimitives.WriteUInt16LittleEndian(header[8..10], Version);
        BinaryPrimitives.WriteUInt16LittleEndian(header[10..12], Mode);
        BinaryPrimitives.WriteUInt16LittleEndian(header[12..14], (ushort)channels);
        BinaryPrimitives.WriteUInt32LittleEndian(header[14..18], width);
        BinaryPrimitives.WriteUInt32LittleEndian(header[18..22], height);
        BinaryPrimitives.WriteUInt16LittleEndian(header[22..24], Offset);

        rdiOutput.Write(header);

        for (int i = 0; i < Offset; ++i)
            rdiOutput.WriteByte(0);

        rdiOutput.Write(data);
    }
}
