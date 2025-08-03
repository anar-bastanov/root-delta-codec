using RdcEngine.Image.Implementations;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace RdcEngine.Image;

internal static class RootDeltaImageTransform
{
    private static readonly Dictionary<int, ImageTransformImpl> Implementations = [];

    public static RawImage Encode(RawImage rawImage, ushort mode)
    {
        var (_, _, _, colorSpace, _, _) = rawImage;
        var impl = GetImplementation(mode, colorSpace);

        return impl.Encode(rawImage);
    }

    public static RawImage Decode(RawImage rawImage, ushort mode)
    {
        var (width, height, _, colorSpace, size, _) = rawImage;
        var impl = GetImplementation(mode, colorSpace);

        if (impl.ComputeLength(width, height) > size)
            throw new InvalidDataException("RDI size mismatch or incomplete pixel data");

        return impl.Decode(rawImage);
    }

    private static ImageTransformImpl GetImplementation(ushort mode, int colorSpace)
    {
        if (mode is 0)
            mode = ImageTransformImpl.DefaultMode;

        int key = (colorSpace << 16) | (mode << 0);
        ref var t = ref CollectionsMarshal.GetValueRefOrAddDefault(Implementations, key, out _);

        return t ??= ImageTransformImpl.Resolve(mode, colorSpace);
    }
}
