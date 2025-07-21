using System;
using System.IO;

namespace RdcEngine;

internal static class RootDeltaTransform
{
    public static RawImage EncodeImage(RawImage rawImage)
    {
        var (width, height, stride, channels, data) = rawImage;
        int length = checked((int)ComputeLength(width, height, channels));

        byte[] rdi = GC.AllocateUninitializedArray<byte>(length);

        for (int y = 0; y < height / 2; ++y)
        {
            for (int x = 0; x < width / 2; ++x)
            {
                rdi[y * (width / 2 * channels) + x * channels + 0] = (byte)(data[y * 2 * stride + x * 2 * channels + 0] * 1);
                rdi[y * (width / 2 * channels) + x * channels + 1] = (byte)(data[y * 2 * stride + x * 2 * channels + 1] * 1);
                rdi[y * (width / 2 * channels) + x * channels + 2] = (byte)(data[y * 2 * stride + x * 2 * channels + 2] * 1);
            }
        }

        return rawImage with { Data = rdi };
    }

    public static RawImage DecodeImage(RawImage rawImage)
    {
        var (width, height, stride, channels, data) = rawImage;
        int length = checked((int)ComputeLength(width, height, channels));

        if (length != data.Length)
            throw new InvalidDataException("RDI size mismatch or pixel data is incomplete");

        byte[] rdi = GC.AllocateUninitializedArray<byte>((int)(stride * height));

        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                rdi[y * stride + x * channels + 0] = (byte)(data[y / 2 * (width / 2 * channels) + x / 2 * channels + 0] * 1);
                rdi[y * stride + x * channels + 1] = (byte)(data[y / 2 * (width / 2 * channels) + x / 2 * channels + 1] * 1);
                rdi[y * stride + x * channels + 2] = (byte)(data[y / 2 * (width / 2 * channels) + x / 2 * channels + 2] * 1);
            }
        }

        return rawImage with { Data = rdi };
    }

    public static ulong ComputeLength(uint width, uint height, uint channels)
    {
        return height / 2 * width / 2 * channels;
    }
}
