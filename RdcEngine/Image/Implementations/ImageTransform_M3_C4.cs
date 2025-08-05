using System;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    private sealed class ImageTransform_M3_C4 : ImageTransformImpl
    {
        public override RawImage Encode(RawImage rawImage)
        {
            var (width, height, stride, _, _, data) = rawImage;
            int length = ComputeLength(width, height);

            byte[] rdi = GC.AllocateUninitializedArray<byte>(length);

            for (int y = 0; y < height; ++y)
            {
                int dataOff = y * stride;
                int rdiOffR = height * 4 + height * (width - 1) * 0;
                int rdiOffG = height * 4 + height * (width - 1) * 1;
                int rdiOffB = height * 4 + height * (width - 1) * 2;
                int rdiOffA = height * 4 + height * (width - 1) * 3;

                byte r = rdi[height * 0 + y] = data[dataOff + 0];
                byte g = rdi[height * 1 + y] = data[dataOff + 1];
                byte b = rdi[height * 2 + y] = data[dataOff + 2];
                byte a = rdi[height * 3 + y] = data[dataOff + 3];

                for (int x = 1; x < width; ++x)
                {
                    byte rd = Utils.ToRootDelta(r, data[dataOff + x * 4 + 0]);
                    byte gd = Utils.ToRootDelta(g, data[dataOff + x * 4 + 1]);
                    byte bd = Utils.ToRootDelta(b, data[dataOff + x * 4 + 2]);
                    byte ad = Utils.ToRootDelta(a, data[dataOff + x * 4 + 3]);

                    rdi[rdiOffR + y * (width - 1) + x - 1] = rd;
                    rdi[rdiOffG + y * (width - 1) + x - 1] = gd;
                    rdi[rdiOffB + y * (width - 1) + x - 1] = bd;
                    rdi[rdiOffA + y * (width - 1) + x - 1] = ad;

                    r += Utils.FromRootDelta(rd);
                    g += Utils.FromRootDelta(gd);
                    b += Utils.FromRootDelta(bd);
                    a += Utils.FromRootDelta(ad);
                }
            }

            return rawImage with { Size = length, Data = rdi };
        }

        public override RawImage Decode(RawImage rawImage)
        {
            var (width, height, stride, _, _, data) = rawImage;

            byte[] raw = GC.AllocateUninitializedArray<byte>(stride * height);

            for (int y = 0; y < height; ++y)
            {
                int dataOffR = height * 4 + height * (width - 1) * 0;
                int dataOffG = height * 4 + height * (width - 1) * 1;
                int dataOffB = height * 4 + height * (width - 1) * 2;
                int dataOffA = height * 4 + height * (width - 1) * 3;
                int rawOff = y * stride;

                byte r = raw[rawOff + 0] = data[height * 0 + y];
                byte g = raw[rawOff + 1] = data[height * 1 + y];
                byte b = raw[rawOff + 2] = data[height * 2 + y];
                byte a = raw[rawOff + 3] = data[height * 3 + y];

                for (int x = 1; x < width; ++x)
                {
                    r += Utils.FromRootDelta(data[dataOffR + y * (width - 1) + x - 1]);
                    g += Utils.FromRootDelta(data[dataOffG + y * (width - 1) + x - 1]);
                    b += Utils.FromRootDelta(data[dataOffB + y * (width - 1) + x - 1]);
                    a += Utils.FromRootDelta(data[dataOffA + y * (width - 1) + x - 1]);

                    raw[rawOff + x * 4 + 0] = r;
                    raw[rawOff + x * 4 + 1] = g;
                    raw[rawOff + x * 4 + 2] = b;
                    raw[rawOff + x * 4 + 3] = a;
                }
            }

            return rawImage with { Size = raw.Length, Data = raw };
        }

        public override int ComputeLength(int width, int height)
        {
            return height * width * 4;
        }
    }
}
