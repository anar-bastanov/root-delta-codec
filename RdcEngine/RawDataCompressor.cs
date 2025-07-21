using System.IO;
using System.IO.Compression;

namespace RdcEngine;

internal static class RawDataCompressor
{
    public static byte[] Compress(byte[] input)
    {
        using var output = new MemoryStream();

        using (Stream compressor = new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
            compressor.Write(input, 0, input.Length);

        return output.ToArray();
    }

    public static byte[] Decompress(byte[] input)
    {
        using var inputStream = new MemoryStream(input);
        using Stream decompressor = new ZLibStream(inputStream, CompressionMode.Decompress);

        using var output = new MemoryStream();
        decompressor.CopyTo(output);

        return output.ToArray();
    }
}
