using System;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    private sealed class ImageTransform_M5_C3 : ImageTransformImpl
    {
        public override RawImage Encode(RawImage rawImage)
        {
            var (width, height, stride, _, _, data) = rawImage;
            int length = ComputeLength(width, height);

            byte[] rdi = GC.AllocateUninitializedArray<byte>(length);

            for (int y = 0; y < height; ++y)
            {
                int dataOff = y * stride;
                int rdiOffL  = height * 3 + height * (width - 1) * 0;
                int rdiOffCo = height * 3 + height * (width - 1) * 1;
                int rdiOffCg = height * 3 + height * (width - 1) * 2;

                var (l, co, cg) = Utils.RgbToYCoCg(data[dataOff + 2], data[dataOff + 1], data[dataOff + 0]);

                rdi[height * 0 + y] = l;
                rdi[height * 1 + y] = co;
                rdi[height * 2 + y] = cg;

                for (int x = 1; x < width; ++x)
                {
                    var (ln, con, cgn) = Utils.RgbToYCoCg(data[dataOff + x * 3 + 2], data[dataOff + x * 3 + 1], data[dataOff + x * 3 + 0]);

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

            return rawImage with { Size = length, Data = rdi };
        }

        public override RawImage Decode(RawImage rawImage)
        {
            var (width, height, stride, _, _, data) = rawImage;

            byte[] raw = GC.AllocateUninitializedArray<byte>(stride * height);

            for (int y = 0; y < height; ++y)
            {
                int dataOffL  = height * 3 + height * (width - 1) * 0;
                int dataOffCo = height * 3 + height * (width - 1) * 1;
                int dataOffCg = height * 3 + height * (width - 1) * 2;
                int rawOff = y * stride;

                byte l  = raw[rawOff + 2] = data[height * 0 + y];
                byte co = raw[rawOff + 1] = data[height * 1 + y];
                byte cg = raw[rawOff + 0] = data[height * 2 + y];

                for (int x = 1; x < width; ++x)
                {
                    l  += Utils.FromRootDelta(data[dataOffL  + y * (width - 1) + x - 1]);
                    co += Utils.FromRootDelta(data[dataOffCo + y * (width - 1) + x - 1]);
                    cg += Utils.FromRootDelta(data[dataOffCg + y * (width - 1) + x - 1]);

                    var (r, g, b) = Utils.YCoCgToRgba(l, co, cg);

                    raw[rawOff + x * 3 + 2] = r;
                    raw[rawOff + x * 3 + 1] = g;
                    raw[rawOff + x * 3 + 0] = b;
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
