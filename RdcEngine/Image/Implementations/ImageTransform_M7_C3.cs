using System;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    private sealed class ImageTransform_M7_C3 : ImageTransformImpl
    {
        public override RawImage Encode(RawImage rawImage)
        {
            var (width, height, stride, _, _, data) = rawImage;
            int length = ComputeLength(width, height);

            byte[] rdi = GC.AllocateUninitializedArray<byte>(length);

            bool vertical = height >= width;
            int pLen = vertical ? height : width;
            int sLen = vertical ? width : height;

            Func<int, int, int> pixelOffset = vertical ?
                (int p, int s) => p * stride + s * 3 :
                (int p, int s) => s * stride + p * 3;

            int headerSize = sLen * 3;
            int channelBlockSize = sLen * (pLen - 1);

            int baseL  = headerSize + channelBlockSize * 0;
            int baseCo = headerSize + channelBlockSize * 1;
            int baseCg = headerSize + channelBlockSize * 2;

            for (int s = 0; s < sLen; ++s)
            {
                int off = pixelOffset(0, s);
                byte rn = data[off + 2];
                byte gn = data[off + 1];
                byte bn = data[off + 0];

                var (l, co, cg) = Utils.RgbToYCoCg(rn, gn, bn);

                rdi[0 * sLen + s] = l;
                rdi[1 * sLen + s] = co;
                rdi[2 * sLen + s] = cg;

                for (int p = 1; p < pLen; ++p)
                {
                    int off2 = pixelOffset(p, s);
                    rn = data[off2 + 2];
                    gn = data[off2 + 1];
                    bn = data[off2 + 0];

                    var (ln, con, cgn) = Utils.RgbToYCoCg(rn, gn, bn);

                    byte ld  = Utils.ToRootDelta(l,  ln);
                    byte cod = Utils.ToRootDelta(co, con);
                    byte cgd = Utils.ToRootDelta(cg, cgn);

                    int off3 = s * (pLen - 1) + (p - 1);
                    rdi[baseL  + off3] = ld;
                    rdi[baseCo + off3] = cod;
                    rdi[baseCg + off3] = cgd;

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

            byte[] raw = GC.AllocateUninitializedArray<byte>(height * stride);

            bool vertical = height >= width;
            int pLen = vertical ? height : width;
            int secondaryLen = vertical ? width : height;

            int headerSize = 3 * secondaryLen;
            int channelBlockSize = secondaryLen * (pLen - 1);

            int baseL  = headerSize + channelBlockSize * 0;
            int baseCo = headerSize + channelBlockSize * 1;
            int baseCg = headerSize + channelBlockSize * 2;

            Func<int, int, int> pixelOffset = vertical ?
                (int p, int s) => p * stride + s * 3 :
                (int p, int s) => s * stride + p * 3;

            for (int s = 0; s < secondaryLen; ++s)
            {
                byte l  = data[0 * secondaryLen + s];
                byte co = data[1 * secondaryLen + s];
                byte cg = data[2 * secondaryLen + s];

                var (r, g, b) = Utils.YCoCgToRgb(l, co, cg);

                int off0 = pixelOffset(0, s);
                raw[off0 + 2] = r;
                raw[off0 + 1] = g;
                raw[off0 + 0] = b;

                for (int p = 1; p < pLen; ++p)
                {
                    int deltaIndex = s * (pLen - 1) + (p - 1);

                    byte ld  = data[baseL  + deltaIndex];
                    byte cod = data[baseCo + deltaIndex];
                    byte cgd = data[baseCg + deltaIndex];

                    l  += Utils.FromRootDelta(ld);
                    co += Utils.FromRootDelta(cod);
                    cg += Utils.FromRootDelta(cgd);

                    (r, g, b) = Utils.YCoCgToRgb(l, co, cg);

                    int off2 = pixelOffset(p, s);
                    raw[off2 + 2] = r;
                    raw[off2 + 1] = g;
                    raw[off2 + 0] = b;
                }
            }

            return rawImage with { Size = raw.Length, Data = raw };
        }

        public override int ComputeLength(int width, int height)
        {
            return width * height * 3;
        }
    }
}
