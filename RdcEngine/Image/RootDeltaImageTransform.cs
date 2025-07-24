using RdcEngine.Image.Implementations;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RdcEngine.Image;

internal static class RootDeltaImageTransform
{
    private static readonly Dictionary<int, ImageTransformImpl> Implementations = [];

    public static RawImage Encode(RawImage rawImage, ushort version, ushort mode) =>
        GetImplementation(version, mode).Encode(rawImage);

    public static RawImage Decode(RawImage rawImage, ushort version, ushort mode) =>
        GetImplementation(version, mode).Decode(rawImage);

    private static ImageTransformImpl GetImplementation(ushort version, ushort mode)
    {
        int key = (version << 16) | (mode << 0);
        ref var t = ref CollectionsMarshal.GetValueRefOrAddDefault(Implementations, key, out _);

        return t ??= ImageTransformImpl.Choose(version, mode);
    }
}
