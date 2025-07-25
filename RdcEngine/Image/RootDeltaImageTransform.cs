using RdcEngine.Image.Implementations;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RdcEngine.Image;

internal static class RootDeltaImageTransform
{
    private static readonly Dictionary<ulong, ImageTransformImpl> Implementations = [];

    public static RawImage Encode(RawImage rawImage, ushort version, ushort mode) =>
        GetImplementation(version, mode, rawImage.Channels).Encode(rawImage);

    public static RawImage Decode(RawImage rawImage, ushort version, ushort mode) =>
        GetImplementation(version, mode, rawImage.Channels).Decode(rawImage);

    private static ImageTransformImpl GetImplementation(ushort version, ushort mode, uint channels)
    {
        ulong key = ((ulong)channels << 32) | ((uint)version << 16) | ((uint)mode << 0);
        ref var t = ref CollectionsMarshal.GetValueRefOrAddDefault(Implementations, key, out _);

        return t ??= ImageTransformImpl.Choose(version, mode, channels);
    }
}
