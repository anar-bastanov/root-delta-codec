using System;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    private sealed class ImageTransform_M9_C4 : ImageTransformImpl
    {
        public override RawImage Encode(RawImage rawImage)
        {
            var (width, height, stride, _, _, data) = rawImage;

            int widthC = (width + 1) / 2;
            int heightC = (height + 1) / 2;

            int headerSizeA = height;
            int headerSizeL = height;
            int headerSizeC = heightC;
            int channelBlockSizeA = height * (width - 1);
            int channelBlockSizeL = height * (width - 1);
            int channelBlockSizeC = heightC * (widthC - 1);

            int size = (headerSizeL + channelBlockSizeL + headerSizeC + channelBlockSizeC) * 2;
            byte[] rdi = GC.AllocateUninitializedArray<byte>(size);

            int offA1  = 0;
            int offL1  = offA1  + headerSizeA;
            int offCo1 = offL1  + headerSizeL;
            int offCg1 = offCo1 + headerSizeC;
            int offA2  = offCg1 + headerSizeC;
            int offL2  = offA2  + channelBlockSizeA;
            int offCo2 = offL2  + channelBlockSizeL;
            int offCg2 = offCo2 + channelBlockSizeC;

            for (int y = 0; y < height; ++y)
            {
                int rowBase = y * stride;
                byte rn = data[rowBase + 2];
                byte gn = data[rowBase + 1];
                byte bn = data[rowBase + 0];
                byte a  = data[rowBase + 3];

                var (l, _, _) = Utils.RgbToYCoCg(rn, gn, bn);

                rdi[offA1 + y] = a;
                rdi[offL1 + y] = l;

                for (int x = 1; x < width; ++x)
                {
                    int px = rowBase + x * 4;
                    rn = data[px + 2];
                    gn = data[px + 1];
                    bn = data[px + 0];
                    byte an = data[px + 3];

                    var (ln, _, _) = Utils.RgbToYCoCg(rn, gn, bn);

                    // byte rn2 = x + 1 < width ? data[px + 2 + 4] : rn;
                    // byte gn2 = x + 1 < width ? data[px + 1 + 4] : gn;
                    // byte bn2 = x + 1 < width ? data[px + 0 + 4] : bn;
                    // byte an2 = x + 1 < width ? data[px + 3 + 4] : an;

                    // var (ln2, _, _) = Utils.RgbToYCoCg(rn2, gn2, bn2);

                    byte aTarget = an; // Utils.EstimateForward(a, an, an2);
                    byte lTarget = ln; // Utils.EstimateForward(l, ln, ln2);

                    byte ad = Utils.ToRootDelta(a, aTarget);
                    byte ld = Utils.ToRootDelta(l, lTarget);

                    rdi[offA2 + y * (width - 1) + x - 1] = ad;
                    rdi[offL2 + y * (width - 1) + x - 1] = ld;

                    a += Utils.FromRootDelta(ad);
                    l += Utils.FromRootDelta(ld);
                }
            }

            for (int y = 0; y < heightC; ++y)
            {
                int y0 = y * 2;
                int y1 = y0 + 1 < height ? y0 + 1 : y0;

                int x0 = 0;
                int x1 = 1 < width ? 1 : 0;

                int off00 = y0 * stride + x0 * 4;
                int off01 = y0 * stride + x1 * 4;
                int off10 = y1 * stride + x0 * 4;
                int off11 = y1 * stride + x1 * 4;

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

                    off00 = y0 * stride + x0 * 4;
                    off01 = y0 * stride + x1 * 4;
                    off10 = y1 * stride + x0 * 4;
                    off11 = y1 * stride + x1 * 4;

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

            for (int i = offA2; i < rdi.Length; i += 2)
            {
                byte a = rdi[i];
                byte b = rdi[i + 1 == rdi.Length ? i : i + 1];
                rdi[offA2 + (i - offA2) / 2] = (byte)(a | (b << 4));
            }

            int length = ComputeLength(width, height);
            return rawImage with { Size = length, Data = rdi };
        }

        public override RawImage Decode(RawImage rawImage)
        {
            var (width, height, stride, _, _, data) = rawImage;

            int widthC = (width + 1) / 2;
            int heightC = (height + 1) / 2;

            int headerSizeA = height;
            int headerSizeL = height;
            int headerSizeC = heightC;
            int channelBlockSizeA = height * (width - 1);
            int channelBlockSizeL = height * (width - 1);
            int channelBlockSizeC = heightC * (widthC - 1);

            int offA1  = 0;
            int offL1  = offA1  + headerSizeA;
            int offCo1 = offL1  + headerSizeL;
            int offCg1 = offCo1 + headerSizeC;
            int offA2  = offCg1 + headerSizeC;
            int offL2  = offA2  + channelBlockSizeA;
            int offCo2 = offL2  + channelBlockSizeL;
            int offCg2 = offCo2 + channelBlockSizeC;

            byte[] raw = GC.AllocateUninitializedArray<byte>(height * stride);
            
            for (int y = 0; y < height; ++y)
            {
                int rowRaw = y * stride;
                int sy = y / 2;
                int syn = sy + 1 < heightC ? sy + 1 : sy;

                byte a = data[offA1 + y];
                byte aTarget = a;

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
                    byte codT = GetNibbleDelta(offCo2 + sy  * (widthC - 1) + coi);
                    byte codB = GetNibbleDelta(offCo2 + syn * (widthC - 1) + coi);
                    byte cgdT = GetNibbleDelta(offCg2 + sy  * (widthC - 1) + cgi);
                    byte cgdB = GetNibbleDelta(offCg2 + syn * (widthC - 1) + cgi);

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

                    int px = rowRaw + x * 4;
                    raw[px + 2] = r;
                    raw[px + 1] = g;
                    raw[px + 0] = b;
                    raw[px + 3] = aTarget;

                    if (++x == width)
                        break;

                    if ((x & 1) is 0)
                    {
                        if (cgi + 1 < widthC - 1)
                        {
                            ++coi;
                            ++cgi;
                        }

                        coT = conT;
                        coB = conB;
                        cgT = cgnT;
                        cgB = cgnB;
                    }

                    byte ad = GetNibbleDelta(offA2 + y * (width - 1) + x - 1);
                    byte ld = GetNibbleDelta(offL2 + y * (width - 1) + x - 1);

                    // byte ad2 = x + 1 < width - 1 ? GetNibbleDelta(offA2 + y * (width - 1) + x - 1 + 1) : ad;
                    // byte ld2 = x + 1 < width - 1 ? GetNibbleDelta(offL2 + y * (width - 1) + x - 1 + 1) : ld;

                    byte an = (byte)(a + Utils.FromRootDelta(ad));
                    byte ln = (byte)(l + Utils.FromRootDelta(ld));

                    // byte an2 = (byte)(an + Utils.FromRootDelta(ad2));
                    // byte ln2 = (byte)(ln + Utils.FromRootDelta(ld2));

                    aTarget = an; // Utils.EstimateReverse(a, an, an2);
                    lTarget = ln; // Utils.EstimateReverse(l, ln, ln2);

                    a = an;
                    l = ln;
                }
            }

            return rawImage with { Size = raw.Length, Data = raw };

            byte GetNibbleDelta(int index)
            {
                byte packed = data[offA2 + (index - offA2) / 2];

                return (byte)(index % 2 is 0 ? packed & 0x0F : packed >> 4);
            }
        }

        public override int ComputeLength(int width, int height)
        {
            int widthC = (width + 1) / 2;
            int heightC = (height + 1) / 2;

            int headerSizeL = height;
            int headerSizeC = heightC;
            int channelBlockSizeL = height * (width - 1);
            int channelBlockSizeC = heightC * (widthC - 1);

            int headerSize = (headerSizeL + headerSizeC) * 2;
            int channelBlockSize = (channelBlockSizeL + channelBlockSizeC) * 2;
            int packedChannelBlockSize = (channelBlockSize + 1) / 2;

            return headerSize + packedChannelBlockSize;
        }
    }
}
