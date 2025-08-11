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

                byte rn = data[dataOff + 2];
                byte gn = data[dataOff + 1];
                byte bn = data[dataOff + 0];

                var (l, co, cg) = Utils.RgbToYCoCg(rn, gn, bn);
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

                    rn = data[dataOff + x * 4 + 2];
                    gn = data[dataOff + x * 4 + 1];
                    bn = data[dataOff + x * 4 + 0];

                    var (ln, con, cgn) = Utils.RgbToYCoCg(rn, gn, bn);
                    byte an = data[dataOff + x * 4 + 3];

                    ld  = Utils.ToRootDelta(l,  ln);
                    cod = Utils.ToRootDelta(co, con);
                    cgd = Utils.ToRootDelta(cg, cgn);
                    ad  = Utils.ToRootDelta(a,  an);

                    l  += Utils.FromRootDelta(ld);
                    co += Utils.FromRootDelta(cod);
                    cg += Utils.FromRootDelta(cgd);
                    a  += Utils.FromRootDelta(ad);
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

                byte l  = data[dataOff + 0];
                byte co = data[dataOff + 1];
                byte cg = data[dataOff + 2];
                byte a  = data[dataOff + 3];
                int x = 0;

                while (true)
                {
                    var (r, g, b) = Utils.YCoCgToRgb(l, co, cg);

                    raw[rawOff + x * 4 + 2] = r;
                    raw[rawOff + x * 4 + 1] = g;
                    raw[rawOff + x * 4 + 0] = b;
                    raw[rawOff + x * 4 + 3] = a;

                    if (++x >= width)
                        break;
                    
                    byte ld  = data[dataOff + x * 4 + 0];
                    byte cod = data[dataOff + x * 4 + 1];
                    byte cgd = data[dataOff + x * 4 + 2];
                    byte ad  = data[dataOff + x * 4 + 3];

                    l  += Utils.FromRootDelta(ld);
                    co += Utils.FromRootDelta(cod);
                    cg += Utils.FromRootDelta(cgd);
                    a  += Utils.FromRootDelta(ad);
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
 