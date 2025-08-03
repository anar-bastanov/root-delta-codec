using System;
using System.IO;
using System.IO.Compression;

namespace RdcEngine;

internal static class RawDataCompressor
{
    public static (byte[] Output, int Size) Compress(byte[] input, int size)
    {
        using var output = new MemoryStream();

        using (Stream compressor = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
            compressor.Write(input.AsSpan(0, size));

        return (output.GetBuffer(), (int)output.Position);
    }

    public static (byte[] Output, int Size) Decompress(byte[] input, int size, int capacity = 0)
    {
        using var inputStream = new MemoryStream(input, 0, size);
        using Stream decompressor = new ZLibStream(inputStream, CompressionMode.Decompress, leaveOpen: true);

        using var output = new MemoryStream(capacity);
        decompressor.CopyTo(output);

        return (output.GetBuffer(), (int)output.Position);
    }
}
