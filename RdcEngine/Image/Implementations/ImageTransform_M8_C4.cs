using System;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    private sealed class ImageTransform_M8_C4 : ImageTransformImpl
    {
        public override RawImage Encode(RawImage rawImage)
        {
            var (width, height, stride, _, _, data) = rawImage;

            int headerSize = height * 4;

            byte[] rdi = GC.AllocateUninitializedArray<byte>(height * width * 4);

            for (int y = 0; y < height; ++y)
            {
                int dataOff = y * stride;
                int rdiOffL  = headerSize + height * (width - 1) * 0;
                int rdiOffCo = headerSize + height * (width - 1) * 1;
                int rdiOffCg = headerSize + height * (width - 1) * 2;
                int rdiOffA  = headerSize + height * (width - 1) * 3;

                byte rn = data[dataOff + 2];
                byte gn = data[dataOff + 1];
                byte bn = data[dataOff + 0];

                var (l, co, cg) = Utils.RgbToYCoCg(rn, gn, bn);
                byte a = data[dataOff + 3];

                rdi[height * 0 + y] = l;
                rdi[height * 1 + y] = co;
                rdi[height * 2 + y] = cg;
                rdi[height * 3 + y] = a;

                for (int x = 1; x < width; ++x)
                {
                    rn = data[dataOff + x * 4 + 2];
                    gn = data[dataOff + x * 4 + 1];
                    bn = data[dataOff + x * 4 + 0];

                    var (ln, con, cgn) = Utils.RgbToYCoCg(rn, gn, bn);
                    byte an = data[dataOff + x * 4 + 3];

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

            for (int i = headerSize; i < rdi.Length; i += 2)
            {
                byte a = rdi[i];
                byte b = rdi[i + 1 == rdi.Length ? i : i + 1];
                rdi[headerSize + (i - headerSize) / 2] = (byte)(a | (b << 4));
            }

            int length = ComputeLength(width, height);
            return rawImage with { Size = length, Data = rdi };
        }

        public override RawImage Decode(RawImage rawImage)
        {
            var (width, height, stride, _, _, data) = rawImage;

            int headerSize = height * 4;

            byte[] raw = GC.AllocateUninitializedArray<byte>(stride * height);

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

                var (r, g, b) = Utils.YCoCgToRgba(l, co, cg);

                raw[rawOff + 2] = r;
                raw[rawOff + 1] = g;
                raw[rawOff + 0] = b;
                raw[rawOff + 3] = a;

                for (int x = 1; x < width; ++x)
                {
                    byte ld  = GetNibbleDelta(dataOffL  + y * (width - 1) + x - 1);
                    byte cod = GetNibbleDelta(dataOffCo + y * (width - 1) + x - 1);
                    byte cgd = GetNibbleDelta(dataOffCg + y * (width - 1) + x - 1);
                    byte ad  = GetNibbleDelta(dataOffA  + y * (width - 1) + x - 1);

                    l  += Utils.FromRootDelta(ld);
                    co += Utils.FromRootDelta(cod);
                    cg += Utils.FromRootDelta(cgd);
                    a  += Utils.FromRootDelta(ad);

                    (r, g, b) = Utils.YCoCgToRgba(l, co, cg);

                    raw[rawOff + x * 4 + 2] = r;
                    raw[rawOff + x * 4 + 1] = g;
                    raw[rawOff + x * 4 + 0] = b;
                    raw[rawOff + x * 4 + 3] = a;
                }
            }

            return rawImage with { Size = raw.Length, Data = raw };

            byte GetNibbleDelta(int index)
            {
                byte packed = data[headerSize + (index - headerSize) / 2];
                return (byte)(index % 2 == 0 ? packed & 0x0F : packed >> 4);
            }
        }

        public override int ComputeLength(int width, int height)
        {
            return height * width * 4;
        }
    }
}
