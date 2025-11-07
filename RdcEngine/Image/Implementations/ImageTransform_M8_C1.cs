using System;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    private sealed class ImageTransform_M8_C1 : ImageTransformImpl
    {
        public override RawImage Encode(RawImage rawImage)
        {
            var (width, height, stride, _, _, data) = rawImage;

            int headerSize = height * 1;

            byte[] rdi = GC.AllocateUninitializedArray<byte>(height * width * 1);

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

            for (int i = headerSize; i < rdi.Length; i += 2)
            {
                byte a = rdi[i];
                byte b = rdi[i + 1 == rdi.Length ? i : i + 1];

                rdi[headerSize + (i - headerSize) / 2] = (byte)(a | (b << 4));
            }

            int length = ComputeLength(width, height);
            return rawImage with { Size = length, Data = rdi };
        }

        public override RawImage Decode(RawImage rawImage)
        {
            var (width, height, stride, _, _, data) = rawImage;

            int headerSize = height * 1;

            byte[] raw = GC.AllocateUninitializedArray<byte>(height * stride);

            for (int y = 0; y < height; ++y)
            {
                int dataOffG = height * 1 + height * (width - 1) * 0;
                int rawOff = y * stride;

                byte g = data[height * 0 + y];

                raw[rawOff + 0] = g;

                for (int x = 1; x < width; ++x)
                {
                    byte gd = GetNibbleDelta(dataOffG + y * (width - 1) + x - 1);

                    byte gn = (byte)(g + Utils.FromRootDelta(gd));

                    g = gn;

                    raw[rawOff + x * 1 + 0] = g;
                }
            }

            return rawImage with { Size = raw.Length, Data = raw };

            byte GetNibbleDelta(int index)
            {
                index -= headerSize;
                byte packed = data[headerSize + index / 2];

                return (byte)(index % 2 is 0 ? packed & 0x0F : packed >> 4);
            }
        }

        public override int ComputeLength(int width, int height)
        {
            int deltaCount = (width - 1) * height * 1;
            int packedDeltaCount = (deltaCount + 1) / 2;

            return height * 1 + packedDeltaCount;
        }
    }
}
