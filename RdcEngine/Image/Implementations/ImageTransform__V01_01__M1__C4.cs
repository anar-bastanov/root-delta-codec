using System;
using System.IO;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    private sealed class ImageTransform__V01_01__M1__C4 : ImageTransformImpl
    {
        public override RawImage Encode(RawImage rawImage)
        {
            var (width, height, stride, channels, data) = rawImage;
            int length = ComputeLength(width, height, channels);

            byte[] rdi = GC.AllocateUninitializedArray<byte>(length);

            for (int y = 0; y < height; ++y)
            {
                int dataOff = y * (int)stride;
                int rdiOff = y * (int)width * 4;

                byte r = data[dataOff + 0], g = data[dataOff + 1], b = data[dataOff + 2], a = data[dataOff + 3];
                byte rd = r, gd = g, bd = b, ad = a;
                uint iter = 0;

                while (true)
                {
                    rdi[rdiOff + iter * 4 + 0] = rd;
                    rdi[rdiOff + iter * 4 + 1] = gd;
                    rdi[rdiOff + iter * 4 + 2] = bd;
                    rdi[rdiOff + iter * 4 + 3] = ad;

                    if (++iter >= width)
                        break;

                    rd = Utils.ToRootDelta(r, data[dataOff + iter * 4 + 0]);
                    gd = Utils.ToRootDelta(g, data[dataOff + iter * 4 + 1]);
                    bd = Utils.ToRootDelta(b, data[dataOff + iter * 4 + 2]);
                    ad = Utils.ToRootDelta(a, data[dataOff + iter * 4 + 3]);

                    r += Utils.FromRootDelta(rd);
                    g += Utils.FromRootDelta(gd);
                    b += Utils.FromRootDelta(bd);
                    a += Utils.FromRootDelta(ad);
                }
            }

            return rawImage with { Data = rdi };
        }

        public override RawImage Decode(RawImage rawImage)
        {
            var (width, height, stride, channels, data) = rawImage;
            int length = ComputeLength(width, height, channels);

            if (length != data.Length)
                throw new InvalidDataException("RDI size mismatch or incomplete pixel data");

            byte[] raw = GC.AllocateUninitializedArray<byte>((int)(stride * height));

            for (int y = 0; y < height; ++y)
            {
                int dataOff = y * (int)width * 4;
                int rawOff = y * (int)stride;

                byte r = data[dataOff + 0], g = data[dataOff + 1], b = data[dataOff + 2], a = data[dataOff + 3];
                uint iter = 0;

                while (true)
                {
                    raw[rawOff + iter * 4 + 0] = r;
                    raw[rawOff + iter * 4 + 1] = g;
                    raw[rawOff + iter * 4 + 2] = b;
                    raw[rawOff + iter * 4 + 3] = a;

                    if (++iter >= width)
                        break;

                    r += Utils.FromRootDelta(data[dataOff + iter * 4 + 0]);
                    g += Utils.FromRootDelta(data[dataOff + iter * 4 + 1]);
                    b += Utils.FromRootDelta(data[dataOff + iter * 4 + 2]);
                    a += Utils.FromRootDelta(data[dataOff + iter * 4 + 2]);
                }

            }

            return rawImage with { Data = raw };
        }

        public override int ComputeLength(uint width, uint height, uint channels)
        {
            return checked((int)(height * width * channels));
        }
    }
}
