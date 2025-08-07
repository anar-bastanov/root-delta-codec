using System;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    private sealed class ImageTransform_M4_C4 : ImageTransformImpl
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

                var (l, co, cg) = Utils.RgbToYCoCg(data[dataOff + 2], data[dataOff + 1], data[dataOff + 0]);
                byte a = data[dataOff + 0];
                byte ld = l, cod = co, cgd = cg, ad = a;
                int x = 0;

                while (true)
                {
                    rdi[rdiOff + x * 4 + 0] = ld;
                    rdi[rdiOff + x * 4 + 1] = cod;
                    rdi[rdiOff + x * 4 + 2] = cgd;
                    rdi[rdiOff + x * 4 + 3] = ad;

                    if (++x >= width)
                        break;

                    var (ln, con, cgn) = Utils.RgbToYCoCg(data[dataOff + x * 4 + 2], data[dataOff + x * 4 + 1], data[dataOff + x * 4 + 0]);
                    byte an = data[dataOff + x * 4 + 3];

                    ld = Utils.ToRootDelta(l, ln);
                    cod = Utils.ToRootDelta(co, con);
                    cgd = Utils.ToRootDelta(cg, cgn);
                    ad = Utils.ToRootDelta(a, an);

                    l += Utils.FromRootDelta(ld);
                    co += Utils.FromRootDelta(cod);
                    cg += Utils.FromRootDelta(cgd);
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

                var (l, co, cg) = (data[dataOff + 0], data[dataOff + 1], data[dataOff + 2]);
                byte a = data[dataOff + 3];
                int x = 0;

                while (true)
                {
                    (raw[rawOff + x * 4 + 2], raw[rawOff + x * 4 + 1], raw[rawOff + x * 4 + 0]) = Utils.YCoCgToRgba(l, co, cg);
                    raw[rawOff + x * 4 + 3] = a;

                    if (++x >= width)
                        break;

                    l += Utils.FromRootDelta(data[dataOff + x * 4 + 0]);
                    co += Utils.FromRootDelta(data[dataOff + x * 4 + 1]);
                    cg += Utils.FromRootDelta(data[dataOff + x * 4 + 2]);
                    a += Utils.FromRootDelta(data[dataOff + x * 4 + 3]);
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
 