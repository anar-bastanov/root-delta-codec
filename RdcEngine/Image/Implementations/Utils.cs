using System.Diagnostics;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    private static class Utils
    {
        public static byte ToRootDelta(byte x, byte y)
        {
            return (y - x) switch
            {
                0 => 0b0000,

                >= +255 => 0b1111,
                >= +253 => 0b1110,
                >= +249 => 0b1101,
                >= +241 => 0b1100,
                >= +225 => 0b1011,
                >= +193 => 0b1010,
                >= +161 => 0b1001,
                >= +128 => 0b1000,
                >= +95  => 0b0111,
                >= +63  => 0b0110,
                >= +31  => 0b0101,
                >= +15  => 0b0100,
                >= +7   => 0b0011,
                >= +3   => 0b0010,
                >= +1   => 0b0001,

                <= -255 => 0b0001,
                <= -253 => 0b0010,
                <= -249 => 0b0011,
                <= -241 => 0b0100,
                <= -225 => 0b0101,
                <= -193 => 0b0110,
                <= -161 => 0b0111,
                <= -128 => 0b1000,
                <= -95  => 0b1001,
                <= -63  => 0b1010,
                <= -31  => 0b1011,
                <= -15  => 0b1100,
                <= -7   => 0b1101,
                <= -3   => 0b1110,
                <= -1   => 0b1111
            };
        }

        public static byte FromRootDelta(byte delta)
        {
            return (delta & 0x0F) switch
            {
                0b0000 => 0,
                0b0001 => 1,
                0b0010 => 3,
                0b0011 => 7,
                0b0100 => 15,
                0b0101 => 31,
                0b0110 => 63,
                0b0111 => 95,
                0b1000 => 128,
                0b1001 => 161,
                0b1010 => 193,
                0b1011 => 225,
                0b1100 => 241,
                0b1101 => 249,
                0b1110 => 253,
                0b1111 => 255,
                _ => throw new UnreachableException()
            };
        }

        public static (byte Y, byte Co, byte Cg) RgbToYCoCg(byte r, byte g, byte b)
        {
            int y = (2 * g + r + b + 1) >> 2;
            int co = ((r - b) >> 1) + 128;
            int cg = ((2 * g - r - b + 1) >> 2) + 128;

            return (ClampToByte(y), ClampToByte(co), ClampToByte(cg));
        }

        public static (byte R, byte G, byte B) YCoCgToRgb(byte y, byte co, byte cg)
        {
            int r = y + co - cg;
            int g = y + cg - 128;
            int b = y - co - cg + 256;

            return (ClampToByte(r), ClampToByte(g), ClampToByte(b));
        }

        private static byte ClampToByte(int value)
        {
            return (byte)(value < 0 ? 0 : value > 255 ? 255 : value);
        }
    }
}
