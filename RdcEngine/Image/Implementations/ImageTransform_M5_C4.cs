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

                    // byte rn2 = x + 1 < width ? data[dataOff + x * 4 + 2 + 4] : rn;
                    // byte gn2 = x + 1 < width ? data[dataOff + x * 4 + 1 + 4] : gn;
                    // byte bn2 = x + 1 < width ? data[dataOff + x * 4 + 0 + 4] : bn;
                    // byte an2 = x + 1 < width ? data[dataOff + x * 4 + 3 + 4] : an;

                    // var (ln2, con2, cgn2) = Utils.RgbToYCoCg(rn2, gn2, bn2);

                    byte lTarget  = ln;  // Utils.EstimateForward(l,  ln,  ln2);
                    byte coTarget = con; // Utils.EstimateForward(co, con, con2);
                    byte cgTarget = cgn; // Utils.EstimateForward(cg, cgn, cgn2);
                    byte aTarget  = an;  // Utils.EstimateForward(a,  an,  an2);

                    byte ld  = Utils.ToRootDelta(l,  lTarget);
                    byte cod = Utils.ToRootDelta(co, coTarget);
                    byte cgd = Utils.ToRootDelta(cg, cgTarget);
                    byte ad  = Utils.ToRootDelta(a,  aTarget);

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

                    // byte ld2  = x + 1 < width ? data[dataOffL  + y * (width - 1) + x - 1 + 4] : ld;
                    // byte cod2 = x + 1 < width ? data[dataOffCo + y * (width - 1) + x - 1 + 4] : cod;
                    // byte cgd2 = x + 1 < width ? data[dataOffCg + y * (width - 1) + x - 1 + 4] : cgd;
                    // byte ad2  = x + 1 < width ? data[dataOffA  + y * (width - 1) + x - 1 + 4] : ad;

                    byte ln  = (byte)(l  + Utils.FromRootDelta(ld));
                    byte con = (byte)(co + Utils.FromRootDelta(cod));
                    byte cgn = (byte)(cg + Utils.FromRootDelta(cgd));
                    byte an  = (byte)(a  + Utils.FromRootDelta(ad));

                    // byte ln2  = (byte)(ln  + Utils.FromRootDelta(ld2));
                    // byte con2 = (byte)(con + Utils.FromRootDelta(cod2));
                    // byte cgn2 = (byte)(cgn + Utils.FromRootDelta(cgd2));
                    // byte an2  = (byte)(an  + Utils.FromRootDelta(ad2));

                    byte lTarget = ln;   // Utils.EstimateReverse(l,  ln,  ln2);
                    byte coTarget = con; // Utils.EstimateReverse(co, con, con2);
                    byte cgTarget = cgn; // Utils.EstimateReverse(cg, cgn, cgn2);
                    byte aTarget  = an;  // Utils.EstimateReverse(a,  an,  an2);

                    l  = ln;
                    co = con;
                    cg = cgn;
                    a  = an;

                    (r, g, b) = Utils.YCoCgToRgb(lTarget, coTarget, cgTarget);

                    raw[rawOff + x * 4 + 2] = r;
                    raw[rawOff + x * 4 + 1] = g;
                    raw[rawOff + x * 4 + 0] = b;
                    raw[rawOff + x * 4 + 3] = aTarget;
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
