using System;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    private sealed class ImageTransform_M5_C1 : ImageTransformImpl
    {
        public override RawImage Encode(RawImage rawImage)
        {
            var (width, height, stride, _, _, data) = rawImage;
            int length = ComputeLength(width, height);

            byte[] rdi = GC.AllocateUninitializedArray<byte>(length);

            for (int y = 0; y < height; ++y)
            {
                int dataOff = y * stride;
                int rdiOffG = height * 1 + height * (width - 1) * 0;

                byte g = data[dataOff + 0];

                rdi[height * 0 + y] = g;

                for (int x = 1; x < width; ++x)
                {
                    byte gn = data[dataOff + x * 1 + 0];

                    byte gd = Utils.ToRootDelta(g, gn);

                    rdi[rdiOffG + y * (width - 1) + x - 1] = gd;

                    g += Utils.FromRootDelta(gd);
                }
            }

            return rawImage with { Size = length, Data = rdi };
        }

        public override RawImage Decode(RawImage rawImage)
        {
            var (width, height, stride, _, _, data) = rawImage;

            byte[] raw = GC.AllocateUninitializedArray<byte>(height * stride);

            for (int y = 0; y < height; ++y)
            {
                int dataOffG = height * 1 + height * (width - 1) * 0;
                int rawOff = y * stride;

                byte g = data[height * 0 + y];

                raw[rawOff + 0] = g;

                for (int x = 1; x < width; ++x)
                {
                    byte gd = data[dataOffG + y * (width - 1) + x - 1];

                    byte gn = (byte)(g + Utils.FromRootDelta(gd));

                    g = gn;

                    raw[rawOff + x * 1 + 0] = gn;
                }
            }

            return rawImage with { Size = raw.Length, Data = raw };
        }

        public override int ComputeLength(int width, int height)
        {
            return height * width * 1;
        }
    }
}
