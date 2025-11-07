using System;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    private sealed class ImageTransform_M8_C3 : ImageTransformImpl
    {
        public override RawImage Encode(RawImage rawImage)
        {
            var (width, height, stride, _, _, data) = rawImage;

            int headerSize = height * 3;

            byte[] rdi = GC.AllocateUninitializedArray<byte>(height * width * 3);

            for (int y = 0; y < height; ++y)
            {
                int dataOff = y * stride;
                int rdiOffL  = headerSize + height * (width - 1) * 0;
                int rdiOffCo = headerSize + height * (width - 1) * 1;
                int rdiOffCg = headerSize + height * (width - 1) * 2;

                byte rn = data[dataOff + 2];
                byte gn = data[dataOff + 1];
                byte bn = data[dataOff + 0];

                var (l, co, cg) = Utils.RgbToYCoCg(rn, gn, bn);

                rdi[height * 0 + y] = l;
                rdi[height * 1 + y] = co;
                rdi[height * 2 + y] = cg;

                for (int x = 1; x < width; ++x)
                {
                    rn = data[dataOff + x * 3 + 2];
                    gn = data[dataOff + x * 3 + 1];
                    bn = data[dataOff + x * 3 + 0];

                    var (ln, con, cgn) = Utils.RgbToYCoCg(rn, gn, bn);

                    byte ld  = Utils.ToRootDelta(l,  ln);
                    byte cod = Utils.ToRootDelta(co, con);
                    byte cgd = Utils.ToRootDelta(cg, cgn);

                    rdi[rdiOffL  + y * (width - 1) + x - 1] = ld;
                    rdi[rdiOffCo + y * (width - 1) + x - 1] = cod;
                    rdi[rdiOffCg + y * (width - 1) + x - 1] = cgd;

                    l  += Utils.FromRootDelta(ld);
                    co += Utils.FromRootDelta(cod);
                    cg += Utils.FromRootDelta(cgd);
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

            int headerSize = height * 3;

            byte[] raw = GC.AllocateUninitializedArray<byte>(height * stride);

            for (int y = 0; y < height; ++y)
            {
                int dataOffL  = headerSize + height * (width - 1) * 0;
                int dataOffCo = headerSize + height * (width - 1) * 1;
                int dataOffCg = headerSize + height * (width - 1) * 2;
                int rawOff = y * stride;

                byte l  = data[height * 0 + y];
                byte co = data[height * 1 + y];
                byte cg = data[height * 2 + y];

                var (r, g, b) = Utils.YCoCgToRgb(l, co, cg);

                raw[rawOff + 2] = r;
                raw[rawOff + 1] = g;
                raw[rawOff + 0] = b;

                for (int x = 1; x < width; ++x)
                {
                    byte ld  = GetNibbleDelta(dataOffL  + y * (width - 1) + x - 1);
                    byte cod = GetNibbleDelta(dataOffCo + y * (width - 1) + x - 1);
                    byte cgd = GetNibbleDelta(dataOffCg + y * (width - 1) + x - 1);

                    byte ln  = (byte)(l  + Utils.FromRootDelta(ld));
                    byte con = (byte)(co + Utils.FromRootDelta(cod));
                    byte cgn = (byte)(cg + Utils.FromRootDelta(cgd));

                    l  = ln;
                    co = con;
                    cg = cgn;

                    (r, g, b) = Utils.YCoCgToRgb(l, co, cg);

                    raw[rawOff + x * 3 + 2] = r;
                    raw[rawOff + x * 3 + 1] = g;
                    raw[rawOff + x * 3 + 0] = b;
                }
            }

            return rawImage with { Size = raw.Length, Data = raw };

            byte GetNibbleDelta(int index)
            {
                index -= headerSize;
                byte packed = data[headerSize + index / 2];

                return (byte)(index % 2 is 0 ? packed & 0x0F : packed >> 4);
            }
        }

        public override int ComputeLength(int width, int height)
        {
            int deltaCount = (width - 1) * height * 3;
            int packedDeltaCount = (deltaCount + 1) / 2;

            return height * 3 + packedDeltaCount;
        }
    }
}
