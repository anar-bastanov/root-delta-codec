using System;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    private sealed class ImageTransform_M6_C3 : ImageTransformImpl
    {
        public override RawImage Encode(RawImage rawImage)
        {
            var (width, height, stride, _, _, data) = rawImage;

            int widthC = (width + 1) / 2;
            int heightC = (height + 1) / 2;

            int headerSizeL = height;
            int headerSizeC = heightC;
            int channelBlockSizeL = height * (width - 1);
            int channelBlockSizeC = heightC * (widthC - 1);

            int length = headerSizeL + channelBlockSizeL + (headerSizeC + channelBlockSizeC) * 2;
            byte[] rdi = GC.AllocateUninitializedArray<byte>(length);

            int offL1  = 0;
            int offCo1 = offL1  + headerSizeL;
            int offCg1 = offCo1 + headerSizeC;
            int offL2  = offCg1 + headerSizeC;
            int offCo2 = offL2  + channelBlockSizeL;
            int offCg2 = offCo2 + channelBlockSizeC;

            for (int y = 0; y < height; ++y)
            {
                int rowBase = y * stride;
                byte rn = data[rowBase + 2];
                byte gn = data[rowBase + 1];
                byte bn = data[rowBase + 0];

                var (l, _, _) = Utils.RgbToYCoCg(rn, gn, bn);

                rdi[offL1 + y] = l;

                for (int x = 1; x < width; ++x)
                {
                    int px = rowBase + x * 3;
                    rn = data[px + 2];
                    gn = data[px + 1];
                    bn = data[px + 0];

                    var (ln, _, _) = Utils.RgbToYCoCg(rn, gn, bn);

                    // byte rn2 = x + 1 < width ? data[px + 2 + 3] : rn;
                    // byte gn2 = x + 1 < width ? data[px + 1 + 3] : gn;
                    // byte bn2 = x + 1 < width ? data[px + 0 + 3] : bn;

                    // var (ln2, _, _) = Utils.RgbToYCoCg(rn2, gn2, bn2);

                    byte lTarget = ln; // Utils.EstimateForward(l, ln, ln2);

                    byte ld = Utils.ToRootDelta(l, lTarget);

                    rdi[offL2 + y * (width - 1) + x - 1] = ld;

                    l += Utils.FromRootDelta(ld);
                }
            }

            for (int y = 0; y < heightC; ++y)
            {
                int y0 = y * 2;
                int y1 = y0 + 1 < height ? y0 + 1 : y0;

                int x0 = 0;
                int x1 = 1 < width ? 1 : 0;

                int off00 = y0 * stride + x0 * 3;
                int off01 = y0 * stride + x1 * 3;
                int off10 = y1 * stride + x0 * 3;
                int off11 = y1 * stride + x1 * 3;

                var (_, co00, cg00) = Utils.RgbToYCoCg(data[off00 + 2], data[off00 + 1], data[off00 + 0]);
                var (_, co01, cg01) = Utils.RgbToYCoCg(data[off01 + 2], data[off01 + 1], data[off01 + 0]);
                var (_, co10, cg10) = Utils.RgbToYCoCg(data[off10 + 2], data[off10 + 1], data[off10 + 0]);
                var (_, co11, cg11) = Utils.RgbToYCoCg(data[off11 + 2], data[off11 + 1], data[off11 + 0]);

                byte co = (byte)((co00 + co01 + co10 + co11) / 4);
                byte cg = (byte)((cg00 + cg01 + cg10 + cg11) / 4);

                rdi[offCo1 + y] = co;
                rdi[offCg1 + y] = cg;

                for (int x = 1; x < widthC; ++x)
                {
                    x0 = x * 2;
                    x1 = x0 + 1 < width ? x0 + 1 : x0;

                    off00 = y0 * stride + x0 * 3;
                    off01 = y0 * stride + x1 * 3;
                    off10 = y1 * stride + x0 * 3;
                    off11 = y1 * stride + x1 * 3;

                    (_, co00, cg00) = Utils.RgbToYCoCg(data[off00 + 2], data[off00 + 1], data[off00 + 0]);
                    (_, co01, cg01) = Utils.RgbToYCoCg(data[off01 + 2], data[off01 + 1], data[off01 + 0]);
                    (_, co10, cg10) = Utils.RgbToYCoCg(data[off10 + 2], data[off10 + 1], data[off10 + 0]);
                    (_, co11, cg11) = Utils.RgbToYCoCg(data[off11 + 2], data[off11 + 1], data[off11 + 0]);

                    byte con = (byte)((co00 + co01 + co10 + co11) / 4);
                    byte cgn = (byte)((cg00 + cg01 + cg10 + cg11) / 4);

                    byte cod = Utils.ToRootDelta(co, con);
                    byte cgd = Utils.ToRootDelta(cg, cgn);

                    rdi[offCo2 + y * (widthC - 1) + x - 1] = cod;
                    rdi[offCg2 + y * (widthC - 1) + x - 1] = cgd;

                    co += Utils.FromRootDelta(cod);
                    cg += Utils.FromRootDelta(cgd);
                }
            }

            return rawImage with { Size = length, Data = rdi };
        }

        public override RawImage Decode(RawImage rawImage)
        {
            var (width, height, stride, _, _, data) = rawImage;

            int widthC = (width + 1) / 2;
            int heightC = (height + 1) / 2;

            int headerSizeL = height;
            int headerSizeC = heightC;
            int channelBlockSizeL = height * (width - 1);
            int channelBlockSizeC = heightC * (widthC - 1);

            int offL1  = 0;
            int offCo1 = offL1  + headerSizeL;
            int offCg1 = offCo1 + headerSizeC;
            int offL2  = offCg1 + headerSizeC;
            int offCo2 = offL2  + channelBlockSizeL;
            int offCg2 = offCo2 + channelBlockSizeC;

            byte[] raw = GC.AllocateUninitializedArray<byte>(stride * height);

            for (int y = 0; y < height; ++y)
            {
                int rowRaw = y * stride;
                int sy = y / 2;
                int syn = Math.Min(sy + 1, heightC - 1);

                byte l = data[offL1 + y];

                byte lTarget = l;

                byte coT = data[offCo1 + sy];
                byte coB = data[offCo1 + syn];
                int coi = 0;

                byte cgT = data[offCg1 + sy];
                byte cgB = data[offCg1 + syn];
                int cgi = 0;

                int x = 0;

                while (true)
                {
                    byte codT = data[offCo2 + sy  * (widthC - 1) + coi];
                    byte codB = data[offCo2 + syn * (widthC - 1) + coi];
                    byte cgdT = data[offCg2 + sy  * (widthC - 1) + cgi];
                    byte cgdB = data[offCg2 + syn * (widthC - 1) + cgi];

                    byte conT = (byte)(coT + Utils.FromRootDelta(codT));
                    byte conB = (byte)(coB + Utils.FromRootDelta(codB));
                    byte cgnT = (byte)(cgT + Utils.FromRootDelta(cgdT));
                    byte cgnB = (byte)(cgB + Utils.FromRootDelta(cgdB));

                    bool xOdd = (x & 1) is not 0;
                    bool yOdd = (y & 1) is not 0;

                    var (co, cg) = (xOdd, yOdd) switch
                    {
                        (false, false) => (coT , cgT),
                        (true, false)  => ((coT + conT + 1) >> 1, (cgT + cgnT + 1) >> 1),
                        (false, true)  => ((coT + coB  + 1) >> 1, (cgT + cgB  + 1) >> 1),
                        (true, true)   => ((coT + conT + coB + conB + 2) >> 2, (cgT + cgnT + cgB + cgnB + 2) >> 2)
                    };

                    var (r, g, b) = Utils.YCoCgToRgb(lTarget, (byte)co, (byte)cg);

                    int px = rowRaw + x * 3;
                    raw[px + 2] = r;
                    raw[px + 1] = g;
                    raw[px + 0] = b;

                    if (++x == width)
                        break;

                    if ((x & 1) is 0)
                    {
                        ++coi;
                        ++cgi;

                        coT = conT;
                        coB = conB;
                        cgT = cgnT;
                        cgB = cgnB;
                    }

                    byte ld = data[offL2 + y * (width - 1) + x - 1];

                    // byte ld2 = x + 1 < width - 1 ? data[offL2 + y * (width - 1) + x - 1 + 1] : ld;

                    byte ln = (byte)(l + Utils.FromRootDelta(ld));

                    // byte ln2 = (byte)(ln + Utils.FromRootDelta(ld2));

                    lTarget = ln; // Utils.EstimateReverse(l, ln, ln2);

                    l = ln;
                }
            }

            return rawImage with { Size = raw.Length, Data = raw };
        }

        public override int ComputeLength(int width, int height)
        {
            int widthC = (width + 1) / 2;
            int heightC = (height + 1) / 2;

            int headerSizeL = height;
            int headerSizeC = heightC;
            int channelBlockSizeL = height * (width - 1);
            int channelBlockSizeC = heightC * (widthC - 1);

            return headerSizeL + channelBlockSizeL + (headerSizeC + channelBlockSizeC) * 2;
        }
    }
}
