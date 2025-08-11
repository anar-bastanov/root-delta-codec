using System;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    private sealed class ImageTransform_M4_C3 : ImageTransformImpl
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

                byte rn = data[dataOff + 2];
                byte gn = data[dataOff + 1];
                byte bn = data[dataOff + 0];

                var (l, co, cg) = Utils.RgbToYCoCg(rn, gn, bn);
                byte ld = l, cod = co, cgd = cg;
                int x = 0;

                while (true)
                {
                    rdi[rdiOff + x * 3 + 0] = ld;
                    rdi[rdiOff + x * 3 + 1] = cod;
                    rdi[rdiOff + x * 3 + 2] = cgd;

                    if (++x >= width)
                        break;

                    rn = data[dataOff + x * 3 + 2];
                    gn = data[dataOff + x * 3 + 1];
                    bn = data[dataOff + x * 3 + 0];

                    var (ln, con, cgn) = Utils.RgbToYCoCg(rn, gn, bn);

                    ld  = Utils.ToRootDelta(l,  ln);
                    cod = Utils.ToRootDelta(co, con);
                    cgd = Utils.ToRootDelta(cg, cgn);

                    l  += Utils.FromRootDelta(ld);
                    co += Utils.FromRootDelta(cod);
                    cg += Utils.FromRootDelta(cgd);
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

                byte l  = data[dataOff + 0];
                byte co = data[dataOff + 1];
                byte cg = data[dataOff + 2];
                int x = 0;

                while (true)
                {
                    var (r, g, b) = Utils.YCoCgToRgb(l, co, cg);

                    raw[rawOff + x * 3 + 2] = r;
                    raw[rawOff + x * 3 + 1] = g;
                    raw[rawOff + x * 3 + 0] = b;

                    if (++x >= width)
                        break;

                    byte ld  = data[dataOff + x * 3 + 0];
                    byte cod = data[dataOff + x * 3 + 1];
                    byte cgd = data[dataOff + x * 3 + 2];

                    l  += Utils.FromRootDelta(ld);
                    co += Utils.FromRootDelta(cod);
                    cg += Utils.FromRootDelta(cgd);
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
