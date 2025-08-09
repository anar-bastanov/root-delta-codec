using System;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    private sealed class ImageTransform_M1_C4 : ImageTransformImpl
    {
        public override RawImage Encode(RawImage rawImage)
        {
            var (width, height, stride, _, _, data) = rawImage;
            int length = ComputeLength(width, height);

            byte[] rdi = GC.AllocateUninitializedArray<byte>(length);

            for (int y = 0; y < height; ++y)
            {
                int dataOff = y * stride;
                int rdiOff = y * width * 4;

                byte r = data[dataOff + 2], g = data[dataOff + 1], b = data[dataOff + 0], a = data[dataOff + 3];
                byte rd = r, gd = g, bd = b, ad = a;
                int x = 0;

                while (true)
                {
                    rdi[rdiOff + x * 4 + 0] = rd;
                    rdi[rdiOff + x * 4 + 1] = gd;
                    rdi[rdiOff + x * 4 + 2] = bd;
                    rdi[rdiOff + x * 4 + 3] = ad;

                    if (++x >= width)
                        break;

                    byte rn = data[dataOff + x * 4 + 2];
                    byte gn = data[dataOff + x * 4 + 1];
                    byte bn = data[dataOff + x * 4 + 0];
                    byte an = data[dataOff + x * 4 + 3];

                    rd = Utils.ToRootDelta(r, rn);
                    gd = Utils.ToRootDelta(g, gn);
                    bd = Utils.ToRootDelta(b, bn);
                    ad = Utils.ToRootDelta(a, an);

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
                int dataOff = y * width * 4;
                int rawOff = y * stride;

                byte r = data[dataOff + 0], g = data[dataOff + 1], b = data[dataOff + 2], a = data[dataOff + 3];
                int x = 0;

                while (true)
                {
                    raw[rawOff + x * 4 + 2] = r;
                    raw[rawOff + x * 4 + 1] = g;
                    raw[rawOff + x * 4 + 0] = b;
                    raw[rawOff + x * 4 + 3] = a;

                    if (++x >= width)
                        break;

                    byte rd = data[dataOff + x * 4 + 0];
                    byte gd = data[dataOff + x * 4 + 1];
                    byte bd = data[dataOff + x * 4 + 2];
                    byte ad = data[dataOff + x * 4 + 3];

                    r += Utils.FromRootDelta(rd);
                    g += Utils.FromRootDelta(gd);
                    b += Utils.FromRootDelta(bd);
                    a += Utils.FromRootDelta(ad);
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
