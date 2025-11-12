using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    private static class Utils
    {
        private static ReadOnlySpan<byte> D2RD =>
        [
            01, 02, 02, 03, 03, 03, 03, 04, 04, 04, 04, 04, 04, 04, 04, 05, 05, 05, 05, 05, 05, 05, 05, 05,
            05, 05, 05, 05, 05, 05, 05, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06,
            06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 07, 07, 07, 07, 07, 07, 07, 07, 07,
            07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 08,
            08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08,
            08, 08, 08, 08, 08, 08, 08, 08, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09,
            09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 10, 10, 10, 10, 10, 10, 10,
            10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10,
            10, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11,
            11, 11, 11, 11, 11, 11, 11, 11, 11, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12,
            12, 13, 13, 13, 13, 13, 13, 13, 13, 14, 14, 14, 14, 15, 15, 00, 01, 01, 02, 02, 02, 02, 03, 03,
            03, 03, 03, 03, 03, 03, 04, 04, 04, 04, 04, 04, 04, 04, 04, 04, 04, 04, 04, 04, 04, 04, 05, 05,
            05, 05, 05, 05, 05, 05, 05, 05, 05, 05, 05, 05, 05, 05, 05, 05, 05, 05, 05, 05, 05, 05, 05, 05,
            05, 05, 05, 05, 05, 05, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06,
            06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 06, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07,
            07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 07, 08,
            08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08, 08,
            08, 08, 08, 08, 08, 08, 08, 08, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09,
            09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 09, 10, 10, 10, 10, 10, 10, 10, 10,
            10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10,
            11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 12, 12, 12, 12, 12, 12, 12, 12,
            13, 13, 13, 13, 14, 14, 15
        ];

        private static ReadOnlySpan<byte> RD2D =>
        [
            0, 1, 3, 7, 15, 31, 63, 95, 128, 161, 193, 225, 241, 249, 253, 255
        ];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ToRootDelta(byte x, byte y)
        {
            ref byte lut = ref MemoryMarshal.GetReference(D2RD);
            ref byte @base = ref Unsafe.Add(ref lut, 255);
            nint index = (nint)y - x;

            return Unsafe.Add(ref @base, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte FromRootDelta(byte delta)
        {
            ref byte lut = ref MemoryMarshal.GetReference(RD2D);
            nint index = (nint)delta & 0x0F;

            return Unsafe.Add(ref lut, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (byte Y, byte Co, byte Cg) RgbToYCoCg(byte r, byte g, byte b)
        {
            int y  = (g + g + r + b + 2 +   0) / 4;
            int co = (      + r - b + 0 + 256) / 2;
            int cg = (g + g - r - b + 1 + 512) / 4;

            return ((byte)y, (byte)co, (byte)cg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (byte R, byte G, byte B) YCoCgToRgb(byte y, byte co, byte cg)
        {
            int r = y + co - cg +   0;
            int g = y      + cg - 128;
            int b = y - co - cg + 256;

            return (ClampToByte(r), ClampToByte(g), ClampToByte(b));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte ClampToByte(int value)
        {
            return (byte)(value switch
            {
                < 0 => 0,
                > 255 => 255,
                _ => value
            });
        }
    }
}
