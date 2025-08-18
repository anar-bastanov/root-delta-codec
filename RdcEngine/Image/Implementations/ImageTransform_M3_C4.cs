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

                byte r = rdi[height * 0 + y] = data[dataOff + 2];
                byte g = rdi[height * 1 + y] = data[dataOff + 1];
                byte b = rdi[height * 2 + y] = data[dataOff + 0];
                byte a = rdi[height * 3 + y] = data[dataOff + 3];

                for (int x = 1; x < width; ++x)
                {
                    byte rn = data[dataOff + x * 4 + 2];
                    byte gn = data[dataOff + x * 4 + 1];
                    byte bn = data[dataOff + x * 4 + 0];
                    byte an = data[dataOff + x * 4 + 3];

                    byte rd = Utils.ToRootDelta(r, rn);
                    byte gd = Utils.ToRootDelta(g, gn);
                    byte bd = Utils.ToRootDelta(b, bn);
                    byte ad = Utils.ToRootDelta(a, an);

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

            byte[] raw = GC.AllocateUninitializedArray<byte>(height * stride);

            for (int y = 0; y < height; ++y)
            {
                int dataOffR = height * 4 + height * (width - 1) * 0;
                int dataOffG = height * 4 + height * (width - 1) * 1;
                int dataOffB = height * 4 + height * (width - 1) * 2;
                int dataOffA = height * 4 + height * (width - 1) * 3;
                int rawOff = y * stride;

                byte r = raw[rawOff + 2] = data[height * 0 + y];
                byte g = raw[rawOff + 1] = data[height * 1 + y];
                byte b = raw[rawOff + 0] = data[height * 2 + y];
                byte a = raw[rawOff + 3] = data[height * 3 + y];

                for (int x = 1; x < width; ++x)
                {
                    byte rd = data[dataOffR + y * (width - 1) + x - 1];
                    byte gd = data[dataOffG + y * (width - 1) + x - 1];
                    byte bd = data[dataOffB + y * (width - 1) + x - 1];
                    byte ad = data[dataOffA + y * (width - 1) + x - 1];

                    r += Utils.FromRootDelta(rd);
                    g += Utils.FromRootDelta(gd);
                    b += Utils.FromRootDelta(bd);
                    a += Utils.FromRootDelta(ad);

                    raw[rawOff + x * 4 + 2] = r;
                    raw[rawOff + x * 4 + 1] = g;
                    raw[rawOff + x * 4 + 0] = b;
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
