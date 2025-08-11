using System;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    private sealed class ImageTransform_M10_C4 : ImageTransformImpl
    {
        public override RawImage Encode(RawImage rawImage)
        {
            var (width, height, stride, _, _, data) = rawImage;

            byte[] rdi = GC.AllocateUninitializedArray<byte>(height * width * 4);

            bool vertical = height >= width;
            int pLen = vertical ? height : width;
            int sLen = vertical ? width : height;

            Func<int, int, int> pixelOffset = vertical ? 
                (int p, int s) => p * stride + s * 4 :
                (int p, int s) => s * stride + p * 4;

            int headerSize = sLen * 4;
            int channelBlockSize = sLen * (pLen - 1);

            int baseL  = headerSize + channelBlockSize * 0;
            int baseCo = headerSize + channelBlockSize * 1;
            int baseCg = headerSize + channelBlockSize * 2;
            int baseA  = headerSize + channelBlockSize * 3;

            for (int s = 0; s < sLen; ++s)
            {
                int off = pixelOffset(0, s);
                byte rn = data[off + 2];
                byte gn = data[off + 1];
                byte bn = data[off + 0];

                var (l, co, cg) = Utils.RgbToYCoCg(rn, gn, bn);
                byte a  = data[off + 3];

                rdi[0 * sLen + s] = l;
                rdi[1 * sLen + s] = co;
                rdi[2 * sLen + s] = cg;
                rdi[3 * sLen + s] = a;

                for (int p = 1; p < pLen; ++p)
                {
                    int off2 = pixelOffset(p, s);
                    rn = data[off2 + 2];
                    gn = data[off2 + 1];
                    bn = data[off2 + 0];

                    var (ln, con, cgn) = Utils.RgbToYCoCg(rn, gn, bn);
                    byte an = data[off2 + 3];

                    byte ld  = Utils.ToRootDelta(l,  ln);
                    byte cod = Utils.ToRootDelta(co, con);
                    byte cgd = Utils.ToRootDelta(cg, cgn);
                    byte ad  = Utils.ToRootDelta(a,  an);

                    int off3 = s * (pLen - 1) + (p - 1);
                    rdi[baseL  + off3] = ld;
                    rdi[baseCo + off3] = cod;
                    rdi[baseCg + off3] = cgd;
                    rdi[baseA  + off3] = ad;

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

            byte[] raw = GC.AllocateUninitializedArray<byte>(stride * height);

            bool vertical = height >= width;
            int pLen = vertical ? height : width;
            int sLen = vertical ? width : height;

            int headerSize = 4 * sLen;
            int channelBlockSize = sLen * (pLen - 1);

            int baseL  = headerSize + channelBlockSize * 0;
            int baseCo = headerSize + channelBlockSize * 1;
            int baseCg = headerSize + channelBlockSize * 2;
            int baseA  = headerSize + channelBlockSize * 3;

            Func<int, int, int> pixelOffset = vertical ?
                (int p, int s) => p * stride + s * 4 :
                (int p, int s) => s * stride + p * 4;

            for (int s = 0; s < sLen; ++s)
            {
                byte l  = data[0 * sLen + s];
                byte co = data[1 * sLen + s];
                byte cg = data[2 * sLen + s];
                byte a  = data[3 * sLen + s];

                var (r, g, b) = Utils.YCoCgToRgb(l, co, cg);

                int off = pixelOffset(0, s);
                raw[off + 2] = r;
                raw[off + 1] = g;
                raw[off + 0] = b;
                raw[off + 3] = a;

                for (int p = 1; p < pLen; ++p)
                {
                    int off1 = s * (pLen - 1) + (p - 1);
                    byte ld  = GetNibbleDelta(baseL  + off1);
                    byte cod = GetNibbleDelta(baseCo + off1);
                    byte cgd = GetNibbleDelta(baseCg + off1);
                    byte ad  = GetNibbleDelta(baseA  + off1);

                    l  += Utils.FromRootDelta(ld);
                    co += Utils.FromRootDelta(cod);
                    cg += Utils.FromRootDelta(cgd);
                    a  += Utils.FromRootDelta(ad);

                    (r, g, b) = Utils.YCoCgToRgb(l, co, cg);

                    int off2 = pixelOffset(p, s);
                    raw[off2 + 2] = r;
                    raw[off2 + 1] = g;
                    raw[off2 + 0] = b;
                    raw[off2 + 3] = a;
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
            int deltaCount = (width - 1) * height * 4;
            int packedDeltaCount = (deltaCount + 1) / 2;
            return height * 4 + packedDeltaCount;
        }
    }
}
