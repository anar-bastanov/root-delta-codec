using RdcEngine.Exceptions;
using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;

namespace RdcEngine;

internal static class RawDataCompressor
{
    private const int MaxPayloadSize = 1 << 30;

    public static (byte[] Output, int Size) Compress(byte[] input, int size)
    {
        if ((uint)size > (uint)input.Length)
            throw new ArgumentOutOfRangeException(nameof(size));

        int capacity = Math.Max(64 * 1024, Math.Min(size + (size >> 4) + 64, 64 * 1024 * 1024));
        using var output = new MemoryStream(capacity);

        try
        {
            using Stream compressor = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true);

            compressor.Write(input, 0, size);
        }
        catch
        {
            throw new MalformedDataException("Could not compress RDI payload");
        }

        return (output.GetBuffer(), (int)output.Position);
    }

    public static (byte[] Output, int Size) Decompress(byte[] input, int size, int capacity = 64 * 1024)
    {
        if ((uint)size > (uint)input.Length)
            throw new ArgumentOutOfRangeException(nameof(size));

        using var output = new MemoryStream(capacity);

        try
        {
            using var inputStream = new MemoryStream(input, 0, size, writable: false);
            using Stream decompressor = new ZLibStream(inputStream, CompressionMode.Decompress, leaveOpen: true);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024 * 1024);

            // decompressor.CopyTo(output);

            try
            {
                int written = 0, n;

                while ((n = decompressor.Read(buffer, 0, buffer.Length)) > 0)
                {
                    written += n;

                    if (written > MaxPayloadSize)
                        throw new ConstraintViolationException("RDI image too big to process");

                    output.Write(buffer, 0, n);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch (Exception e) when (e is not ConstraintViolationException)
        {
            throw new MalformedFileException("Could not decompress RDI payload");
        }

        return (output.GetBuffer(), (int)output.Position);
    }
}
