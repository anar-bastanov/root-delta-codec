using System;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    private sealed class ImageTransform_M6_C4 : ImageTransformImpl
    {
        public override RawImage Encode(RawImage rawImage)
        {
            var (width, height, stride, _, _, data) = rawImage;

            byte[] rdi = GC.AllocateUninitializedArray<byte>(height * width * 4);
            --width;

            for (int y = 0; y < height; ++y)
            {
                int dataOff = y * stride;
                int rdiOffL  = height * 4 + height * width * 1;
                int rdiOffCo = height * 4 + height * width * 2;
                int rdiOffCg = height * 4 + height * width * 3;
                int rdiOffA  = height * 4 + height * width * 0;

                byte rn = data[dataOff + 2];
                byte gn = data[dataOff + 1];
                byte bn = data[dataOff + 0];

                var (l, co, cg) = Utils.RgbToYCoCg(rn, gn, bn);
                byte a = data[dataOff + 3];

                rdi[height * 1 + y] = l;
                rdi[height * 2 + y] = co;
                rdi[height * 3 + y] = cg;
                rdi[height * 0 + y] = a;

                for (int x = 0; x < width; ++x)
                {
                    rn = data[dataOff + x * 4 + 2 + 4];
                    gn = data[dataOff + x * 4 + 1 + 4];
                    bn = data[dataOff + x * 4 + 0 + 4];

                    var (ln, con, cgn) = Utils.RgbToYCoCg(rn, gn, bn);
                    byte an = data[dataOff + x * 4 + 3 + 4];

                    byte ld = Utils.ToRootDelta(l, ln);
                    byte ad = Utils.ToRootDelta(a, an);

                    rdi[rdiOffL  + y * width + x] = ld;
                    rdi[rdiOffCo + y * width + x] = con;
                    rdi[rdiOffCg + y * width + x] = cgn;
                    rdi[rdiOffA  + y * width + x] = ad;

                    l += Utils.FromRootDelta(ld);
                    a += Utils.FromRootDelta(ad);
                }
            }

            for (int y = 0; y < height; y += 2)
            {
                int dataOffCo = height * 4 + height * width * 2;
                int rdiOffCo = dataOffCo;

                int v1 = y * width;
                int v2 = y + 1 == height ? v1 : v1 + width;

                byte co = rdi[height * 2 + y];

                for (int x = 0; x < width; x += 2)
                {
                    int h1 = x;
                    int h2 = x + 1 == width ? h1 : h1 + 1;

                    byte co1 = rdi[dataOffCo + v1 + h1];
                    byte co2 = rdi[dataOffCo + v1 + h2];
                    byte co3 = rdi[dataOffCo + v2 + h1];
                    byte co4 = rdi[dataOffCo + v2 + h2];
                    byte con = (byte)((co1 + co2 + co3 + co4) / 4);

                    byte cod = Utils.ToRootDelta(co, con);

                    rdi[rdiOffCo + y / 2 * ((width + 1) / 2) + x / 2] = cod;

                    co += Utils.FromRootDelta(cod);
                }
            }

            for (int y = 0; y < height; y += 2)
            {
                int dataOffCg = height * 4 + height * width * 3;
                int rdiOffCg = height * 4 + height * width * 2 + (height + 1) / 2 * ((width + 1) / 2);

                int v1 = y * width;
                int v2 = y + 1 == height ? v1 : v1 + width;

                byte cg = rdi[height * 3 + y];

                for (int x = 0; x < width; x += 2)
                {
                    int h1 = x;
                    int h2 = x + 1 == width ? h1 : h1 + 1;

                    byte cg1 = rdi[dataOffCg + v1 + h1];
                    byte cg2 = rdi[dataOffCg + v1 + h2];
                    byte cg3 = rdi[dataOffCg + v2 + h1];
                    byte cg4 = rdi[dataOffCg + v2 + h2];
                    byte cgn = (byte)((cg1 + cg2 + cg3 + cg4) / 4);

                    byte cgd = Utils.ToRootDelta(cg, cgn);

                    rdi[rdiOffCg + y / 2 * ((width + 1) / 2) + x / 2] = cgd;

                    cg += Utils.FromRootDelta(cgd);
                }
            }

            int length = ComputeLength(width + 1, height);
            return rawImage with { Size = length, Data = rdi };
        }

        public override RawImage Decode(RawImage rawImage)
        {
            var (width, height, stride, _, _, data) = rawImage;

            byte[] raw = GC.AllocateUninitializedArray<byte>(stride * height);
            --width;

            for (int y = 0; y < height; ++y)
            {
                int dataOffL  = height * 4 + height * width;
                int dataOffCo = height * 4 + height * width * 2;
                int dataOffCg = height * 4 + height * width + (height + 1) / 2 * ((width + 1) / 2);
                int dataOffA  = height * 4;
                int rawOff = y * stride;

                byte l  = data[height * 1 + y];
                byte co = data[height * 2 + y];
                byte cg = data[height * 3 + y];
                byte a  = data[height * 0 + y];

                var (r, g, b) = Utils.YCoCgToRgba(l, co, cg);

                raw[rawOff + 2] = r;
                raw[rawOff + 1] = g;
                raw[rawOff + 0] = b;
                raw[rawOff + 3] = a;

                for (int x = 0; x < width; ++x)
                {
                    byte ld = data[dataOffL + y * width + x];
                    byte ad = data[dataOffA + y * width + x];

                    l += Utils.FromRootDelta(ld);
                    a += Utils.FromRootDelta(ad);

                    if ((x & 1) == 0)
                    {
                        byte cod = data[dataOffCo + y / 2 * ((width + 1) / 2) + x / 2];
                        byte cgd = data[dataOffCg + y / 2 * ((width + 1) / 2) + x / 2];

                        co += Utils.FromRootDelta(cod);
                        cg += Utils.FromRootDelta(cgd);
                    }

                    (r, g, b) = Utils.YCoCgToRgba(l, co, cg);

                    raw[rawOff + x * 4 + 2 + 4] = r;
                    raw[rawOff + x * 4 + 1 + 4] = g;
                    raw[rawOff + x * 4 + 0 + 4] = b;
                    raw[rawOff + x * 4 + 3 + 4] = a;
                }
            }

            return rawImage with { Size = raw.Length, Data = raw };
        }

        public override int ComputeLength(int width, int height)
        {
            return height * 4 + height * (width - 1) * 2 + (height + 1) / 2 * (width / 2) * 2;
        }
    }
}
