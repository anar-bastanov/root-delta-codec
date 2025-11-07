using System;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    private sealed class ImageTransform_M5_C4 : ImageTransformImpl
    {
        public override RawImage Encode(RawImage rawImage)
        {
            var (width, height, stride, _, _, data) = rawImage;
            int length = ComputeLength(width, height);

            byte[] rdi = GC.AllocateUninitializedArray<byte>(length);

            for (int y = 0; y < height; ++y)
            {
                int dataOff = y * stride;
                int rdiOffL  = height * 4 + height * (width - 1) * 0;
                int rdiOffCo = height * 4 + height * (width - 1) * 1;
                int rdiOffCg = height * 4 + height * (width - 1) * 2;
                int rdiOffA  = height * 4 + height * (width - 1) * 3;

                byte rn = data[dataOff + 2];
                byte gn = data[dataOff + 1];
                byte bn = data[dataOff + 0];
                byte a  = data[dataOff + 3];

                var (l, co, cg) = Utils.RgbToYCoCg(rn, gn, bn);

                rdi[height * 0 + y] = l;
                rdi[height * 1 + y] = co;
                rdi[height * 2 + y] = cg;
                rdi[height * 3 + y] = a;

                for (int x = 1; x < width; ++x)
                {
                    rn = data[dataOff + x * 4 + 2];
                    gn = data[dataOff + x * 4 + 1];
                    bn = data[dataOff + x * 4 + 0];
                    byte an = data[dataOff + x * 4 + 3];

                    var (ln, con, cgn) = Utils.RgbToYCoCg(rn, gn, bn);

                    byte ld  = Utils.ToRootDelta(l,  ln);
                    byte cod = Utils.ToRootDelta(co, con);
                    byte cgd = Utils.ToRootDelta(cg, cgn);
                    byte ad  = Utils.ToRootDelta(a,  an);

                    rdi[rdiOffL  + y * (width - 1) + x - 1] = ld;
                    rdi[rdiOffCo + y * (width - 1) + x - 1] = cod;
                    rdi[rdiOffCg + y * (width - 1) + x - 1] = cgd;
                    rdi[rdiOffA  + y * (width - 1) + x - 1] = ad;

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

            byte[] raw = GC.AllocateUninitializedArray<byte>(height * stride);

            for (int y = 0; y < height; ++y)
            {
                int dataOffL  = height * 4 + height * (width - 1) * 0;
                int dataOffCo = height * 4 + height * (width - 1) * 1;
                int dataOffCg = height * 4 + height * (width - 1) * 2;
                int dataOffA  = height * 4 + height * (width - 1) * 3;
                int rawOff = y * stride;

                byte l  = data[height * 0 + y];
                byte co = data[height * 1 + y];
                byte cg = data[height * 2 + y];
                byte a  = data[height * 3 + y];

                var (r, g, b) = Utils.YCoCgToRgb(l, co, cg);

                raw[rawOff + 2] = r;
                raw[rawOff + 1] = g;
                raw[rawOff + 0] = b;
                raw[rawOff + 3] = a;

                for (int x = 1; x < width; ++x)
                {
                    byte ld  = data[dataOffL  + y * (width - 1) + x - 1];
                    byte cod = data[dataOffCo + y * (width - 1) + x - 1];
                    byte cgd = data[dataOffCg + y * (width - 1) + x - 1];
                    byte ad  = data[dataOffA  + y * (width - 1) + x - 1];

                    byte ln  = (byte)(l  + Utils.FromRootDelta(ld));
                    byte con = (byte)(co + Utils.FromRootDelta(cod));
                    byte cgn = (byte)(cg + Utils.FromRootDelta(cgd));
                    byte an  = (byte)(a  + Utils.FromRootDelta(ad));

                    l  = ln;
                    co = con;
                    cg = cgn;
                    a  = an;

                    (r, g, b) = Utils.YCoCgToRgb(l, co, cg);

                    raw[rawOff + x * 4 + 2] = r;
                    raw[rawOff + x * 4 + 1] = g;
                    raw[rawOff + x * 4 + 0] = b;
                    raw[rawOff + x * 4 + 3] = a;
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
