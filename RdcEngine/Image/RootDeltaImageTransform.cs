using RdcEngine.Exceptions;
using RdcEngine.Image.Implementations;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RdcEngine.Image;

internal static class RootDeltaImageTransform
{
    private static readonly Dictionary<int, ImageTransformImpl> Implementations = [];

    public static RawImage Encode(RawImage bmp, ref ushort mode)
    {
        if (mode is 0)
            mode = ImageTransformImpl.DefaultMode;

        var impl = GetImplementation(mode, bmp.ColorModel);

        RawImage rdi = impl.Encode(bmp);

        (rdi.Data, rdi.Size) = RawDataCompressor.Compress(rdi.Data, rdi.Size);

        return rdi;
    }

    public static RawImage Decode(RawImage rdi, ushort mode)
    {
        var impl = GetImplementation(mode, rdi.ColorModel);
        int requiredLength = impl.ComputeLength(rdi.Width, rdi.Height);

        (rdi.Data, rdi.Size) = RawDataCompressor.Decompress(rdi.Data, rdi.Size, capacity: requiredLength);

        if (rdi.Size < requiredLength)
            throw new MalformedFileException("RDI file has incomplete pixel data");

        RawImage bmp = impl.Decode(rdi);

        return bmp;
    }

    private static ImageTransformImpl GetImplementation(ushort mode, int colorModel)
    {
        int key = (colorModel << 16) | (mode << 0);
        ref var t = ref CollectionsMarshal.GetValueRefOrAddDefault(Implementations, key, out _);

        return t ??= ImageTransformImpl.Resolve(mode, colorModel);
    }
}
