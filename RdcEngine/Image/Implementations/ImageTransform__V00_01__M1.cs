using System;
using System.IO;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    private sealed class ImageTransform__V00_01__M1 : ImageTransformImpl
    {
        public override RawImage Encode(RawImage rawImage)
        {
            var (width, height, stride, channels, data) = rawImage;
            int length = ComputeLength(width, height, channels);

            byte[] rdi = GC.AllocateUninitializedArray<byte>(length);

            for (int y = 0; y < height / 2; ++y)
                for (int x = 0; x < width / 2; ++x)
                {
                    rdi[y * (width / 2) * channels + x * channels + 0] = (byte)(data[y * 2 * stride + x * 2 * channels + 0] * 1);
                    rdi[y * (width / 2) * channels + x * channels + 1] = (byte)(data[y * 2 * stride + x * 2 * channels + 1] * 1);
                    rdi[y * (width / 2) * channels + x * channels + 2] = (byte)(data[y * 2 * stride + x * 2 * channels + 2] * 1);
                }

            return rawImage with { Data = rdi };
        }

        public override RawImage Decode(RawImage rawImage)
        {
            var (width, height, stride, channels, data) = rawImage;
            int length = ComputeLength(width, height, channels);

            if (length != data.Length)
                throw new InvalidDataException("RDI size mismatch or incomplete pixel data");

            byte[] rdi = GC.AllocateUninitializedArray<byte>((int)(stride * height));

            for (int y = 0; y < height; ++y)
                for (int x = 0; x < width; ++x)
                {
                    rdi[y * stride + x * channels + 0] = (byte)(data[y / 2 * (width / 2) * channels + x / 2 * channels + 0] * 1);
                    rdi[y * stride + x * channels + 1] = (byte)(data[y / 2 * (width / 2) * channels + x / 2 * channels + 1] * 1);
                    rdi[y * stride + x * channels + 2] = (byte)(data[y / 2 * (width / 2) * channels + x / 2 * channels + 2] * 1);
                }

            return rawImage with { Data = rdi };
        }

        public override int ComputeLength(uint width, uint height, uint channels)
        {
            return checked((int)((height + 1) / 2 * ((width + 1) / 2) * channels));
        }
    }
}
