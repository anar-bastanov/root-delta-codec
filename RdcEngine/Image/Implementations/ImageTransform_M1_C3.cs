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

                byte r = data[dataOff + 2];
                byte g = data[dataOff + 1];
                byte b = data[dataOff + 0];

                byte rd = r;
                byte gd = g;
                byte bd = b;

                int x = 0;

                while (true)
                {
                    rdi[rdiOff + x * 3 + 0] = rd;
                    rdi[rdiOff + x * 3 + 1] = gd;
                    rdi[rdiOff + x * 3 + 2] = bd;

                    if (++x >= width)
                        break;

                    byte rn = data[dataOff + x * 3 + 2];
                    byte gn = data[dataOff + x * 3 + 1];
                    byte bn = data[dataOff + x * 3 + 0];

                    // byte rn2 = x + 1 < width ? data[dataOff + x * 3 + 2 + 3] : rn;
                    // byte gn2 = x + 1 < width ? data[dataOff + x * 3 + 1 + 3] : gn;
                    // byte bn2 = x + 1 < width ? data[dataOff + x * 3 + 0 + 3] : bn;

                    byte rTarget = rn; // Utils.EstimateForward(r, rn, rn2);
                    byte gTarget = gn; // Utils.EstimateForward(g, gn, gn2);
                    byte bTarget = bn; // Utils.EstimateForward(b, bn, bn2);

                    rd = Utils.ToRootDelta(r, rTarget);
                    gd = Utils.ToRootDelta(g, gTarget);
                    bd = Utils.ToRootDelta(b, bTarget);

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

                byte r = data[dataOff + 0];
                byte g = data[dataOff + 1];
                byte b = data[dataOff + 2];

                byte rTarget = r;
                byte gTarget = g;
                byte bTarget = b;

                int x = 0;

                while (true)
                {
                    raw[rawOff + x * 3 + 2] = rTarget;
                    raw[rawOff + x * 3 + 1] = gTarget;
                    raw[rawOff + x * 3 + 0] = bTarget;

                    if (++x >= width)
                        break;

                    byte rd = data[dataOff + x * 3 + 0];
                    byte gd = data[dataOff + x * 3 + 1];
                    byte bd = data[dataOff + x * 3 + 2];

                    // byte rd2 = x + 1 < width ? data[dataOff + x * 3 + 0 + 3] : rd;
                    // byte gd2 = x + 1 < width ? data[dataOff + x * 3 + 1 + 3] : gd;
                    // byte bd2 = x + 1 < width ? data[dataOff + x * 3 + 2 + 3] : bd;

                    byte rn = (byte)(r + Utils.FromRootDelta(rd));
                    byte gn = (byte)(g + Utils.FromRootDelta(gd));
                    byte bn = (byte)(b + Utils.FromRootDelta(bd));

                    // byte rn2 = (byte)(rn + Utils.FromRootDelta(rd2));
                    // byte gn2 = (byte)(gn + Utils.FromRootDelta(gd2));
                    // byte bn2 = (byte)(bn + Utils.FromRootDelta(bd2));

                    rTarget = rn; // Utils.EstimateReverse(r, rn, rn2);
                    gTarget = gn; // Utils.EstimateReverse(g, gn, gn2);
                    bTarget = bn; // Utils.EstimateReverse(b, bn, bn2);

                    r = rn;
                    g = gn;
                    b = bn;
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
 