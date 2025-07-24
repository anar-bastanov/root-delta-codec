using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;

namespace RdcEngine.Image.Formats;

internal static class RdiFormat
{
    private const ushort HeaderSize = 24;

    private const ulong Signature = 0x00494452_00524E41;

    private const ushort Offset = 8;

    [SkipLocalsInit]
    public static RawImage Load(Stream rdiInput, out ushort version, out ushort mode)
    {
        StreamValidator.EnsureReadable(rdiInput);
        StreamValidator.EnsureRemaining(rdiInput, HeaderSize);

        Span<byte> header = stackalloc byte[HeaderSize];
        rdiInput.ReadExactly(header);

        ulong  signature = BinaryPrimitives.ReadUInt64LittleEndian(header[00..08]);
               version   = BinaryPrimitives.ReadUInt16BigEndian   (header[08..10]);
               mode      = BinaryPrimitives.ReadUInt16LittleEndian(header[10..12]);
        ushort channels  = BinaryPrimitives.ReadUInt16LittleEndian(header[12..14]);
        uint   width     = BinaryPrimitives.ReadUInt32LittleEndian(header[14..18]);
        uint   height    = BinaryPrimitives.ReadUInt32LittleEndian(header[18..22]);
        ushort offset    = BinaryPrimitives.ReadUInt16LittleEndian(header[22..24]);

        if (signature is not Signature)
            throw new InvalidDataException("Invalid RDI signature");

        if (channels is not 3 and not 4)
            throw new NotSupportedException("Only 24-bit and 32-bit RDI is supported");

        if (width is 0 || height is 0)
            throw new InvalidDataException("Invalid RDI dimensions");

        StreamValidator.EnsureRemaining(rdiInput, offset + 1u);
        rdiInput.Seek(offset, SeekOrigin.Current);

        byte[] raw = GC.AllocateUninitializedArray<byte>(checked((int)(rdiInput.Length - rdiInput.Position)));
        rdiInput.ReadExactly(raw);

        return new RawImage(width, height, checked(width * channels), channels, raw);
    }

    [SkipLocalsInit]
    public static void Save(RawImage rawImage, Stream rdiOutput, ushort version, ushort mode)
    {
        StreamValidator.EnsureWritable(rdiOutput);
        ArgumentNullException.ThrowIfNull(rawImage.Data);

        var (width, height, strideInput, channels, data) = rawImage;

        if (width is 0 || height is 0)
            throw new ArgumentException("Width and height of RDI must be non-zero", nameof(rawImage));

        if (channels is not 3 and not 4)
            throw new ArgumentException("Invalid channel count for RDI", nameof(rawImage));

        Span<byte> header = stackalloc byte[HeaderSize];
        BinaryPrimitives.WriteUInt64LittleEndian(header[00..08], Signature);
        BinaryPrimitives.WriteUInt16BigEndian   (header[08..10], version);
        BinaryPrimitives.WriteUInt16LittleEndian(header[10..12], mode);
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
