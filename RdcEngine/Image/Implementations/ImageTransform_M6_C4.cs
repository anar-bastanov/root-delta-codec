using System;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    private sealed class ImageTransform_M6_C4 : ImageTransformImpl
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

            int length = (headerSizeL + channelBlockSizeL + headerSizeC + channelBlockSizeC) * 2;
            byte[] rdi = GC.AllocateUninitializedArray<byte>(length);

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

                var (l, _, _) = Utils.RgbToYCoCg(rn, gn, bn);
                byte a = data[rowBase + 3];

                rdi[offA1 + y] = a;
                rdi[offL1 + y] = l;

                for (int x = 1; x < width; ++x)
                {
                    int px = rowBase + x * 4;
                    rn = data[px + 2];
                    gn = data[px + 1];
                    bn = data[px + 0];

                    var (ln, _, _) = Utils.RgbToYCoCg(rn, gn, bn);
                    byte an = data[px + 3];

                    byte ad = Utils.ToRootDelta(a, an);
                    byte ld = Utils.ToRootDelta(l, ln);

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

            byte[] raw = GC.AllocateUninitializedArray<byte>(stride * height);

            for (int y = 0; y < height; ++y)
            {
                int rowBase = y * stride;
                int sy = y / 2;

                byte a  = data[offA1  + y];
                byte l  = data[offL1  + y];
                byte co = data[offCo1 + sy];
                byte cg = data[offCg1 + sy];

                var (r, g, b) = Utils.YCoCgToRgb(l, co, cg);
                raw[rowBase + 2] = r;
                raw[rowBase + 1] = g;
                raw[rowBase + 0] = b;
                raw[rowBase + 3] = a;

                int baseCo = offCo2 + sy * (widthC - 1);
                int baseCg = offCg2 + sy * (widthC - 1);

                for (int x = 1; x < width; ++x)
                {
                    byte ad = data[offA2 + y * (width - 1) + x - 1];
                    byte ld = data[offL2 + y * (width - 1) + x - 1];

                    a += Utils.FromRootDelta(ad);
                    l += Utils.FromRootDelta(ld);

                    if (x % 2 == 0 && widthC > 1)
                    {
                        int deltaIndex = x / 2 - 1;
                        byte cod = data[baseCo + deltaIndex];
                        byte cgd = data[baseCg + deltaIndex];

                        co += Utils.FromRootDelta(cod);
                        cg += Utils.FromRootDelta(cgd);
                    }

                    (r, g, b) = Utils.YCoCgToRgb(l, co, cg);

                    int px = rowBase + x * 4;
                    raw[px + 2] = r;
                    raw[px + 1] = g;
                    raw[px + 0] = b;
                    raw[px + 3] = a;
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

            return (headerSizeL + channelBlockSizeL + headerSizeC + channelBlockSizeC) * 2;
        }
    }
}
