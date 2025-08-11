using System;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    private sealed class ImageTransform_M10_C3 : ImageTransformImpl
    {
        public override RawImage Encode(RawImage rawImage)
        {
            var (width, height, stride, _, _, data) = rawImage;

            byte[] rdi = GC.AllocateUninitializedArray<byte>(height * width * 3);

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

            byte[] raw = GC.AllocateUninitializedArray<byte>(stride * height);

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

                    byte ld  = GetNibbleDelta(baseL  + deltaIndex);
                    byte cod = GetNibbleDelta(baseCo + deltaIndex);
                    byte cgd = GetNibbleDelta(baseCg + deltaIndex);

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

            byte GetNibbleDelta(int index)
            {
                byte packed = data[headerSize + (index - headerSize) / 2];
                return (byte)(index % 2 == 0 ? packed & 0x0F : packed >> 4);
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
