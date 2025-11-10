using RdcEngine.Exceptions;
using System;
using System.IO;
using System.IO.Compression;

namespace RdcEngine;

internal static class RawDataCompressor
{
    private const uint MaxPayloadSize = 1u << 30;

    public static (byte[] Output, int Size) Compress(byte[] input, int size)
    {
        try
        {
            using var output = new MemoryStream();

            using (Stream compressor = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
                compressor.Write(input.AsSpan(0, size));

            return (output.GetBuffer(), (int)output.Position);
        }
        catch
        {
            throw new MalformedDataException("Could not compress RDI payload");
        }
    }

    public static (byte[] Output, int Size) Decompress(byte[] input, int size)
    {
        try
        {
            using var inputStream = new MemoryStream(input, 0, size);
            using Stream decompressor = new ZLibStream(inputStream, CompressionMode.Decompress, leaveOpen: true);

            using var output = new MemoryStream();
            decompressor.CopyTo(output);

            if (output.Position > MaxPayloadSize)
                throw new ConstraintViolationException("RDI image too big to decompress");

            return (output.GetBuffer(), (int)output.Position);
        }
        catch (Exception e) when (e is not CodecException)
        {
            throw new MalformedFileException("Could not decompress RDI payload");
        }
    }
}
