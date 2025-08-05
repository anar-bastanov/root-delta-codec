using System;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    private sealed class ImageTransform_M1_C3 : ImageTransformImpl
    {
        public override RawImage Encode(RawImage rawImage)
        {
            var (width, height, stride, _, _, data) = rawImage;
            int length = ComputeLength(width, height);

            byte[] rdi = GC.AllocateUninitializedArray<byte>(length);

            for (int y = 0; y < height; ++y)
            {
                int dataOff = y * stride;
                int rdiOff = y * width * 3;

                byte r = data[dataOff + 0], g = data[dataOff + 1], b = data[dataOff + 2];
                byte rd = r, gd = g, bd = b;
                int x = 0;

                while (true)
                {
                    rdi[rdiOff + x * 3 + 0] = rd;
                    rdi[rdiOff + x * 3 + 1] = gd;
                    rdi[rdiOff + x * 3 + 2] = bd;

                    if (++x >= width)
                        break;

                    rd = Utils.ToRootDelta(r, data[dataOff + x * 3 + 0]);
                    gd = Utils.ToRootDelta(g, data[dataOff + x * 3 + 1]);
                    bd = Utils.ToRootDelta(b, data[dataOff + x * 3 + 2]);

                    r += Utils.FromRootDelta(rd);
                    g += Utils.FromRootDelta(gd);
                    b += Utils.FromRootDelta(bd);
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
                int dataOff = y * width * 3;
                int rawOff = y * stride;

                byte r = data[dataOff + 0], g = data[dataOff + 1], b = data[dataOff + 2];
                int x = 0;

                while (true)
                {
                    raw[rawOff + x * 3 + 0] = r;
                    raw[rawOff + x * 3 + 1] = g;
                    raw[rawOff + x * 3 + 2] = b;

                    if (++x >= width)
                        break;

                    r += Utils.FromRootDelta(data[dataOff + x * 3 + 0]);
                    g += Utils.FromRootDelta(data[dataOff + x * 3 + 1]);
                    b += Utils.FromRootDelta(data[dataOff + x * 3 + 2]);
                }
            }

            return rawImage with { Size = raw.Length, Data = raw };
        }

        public override int ComputeLength(int width, int height)
        {
            return height * width * 3;
        }
    }
}
 