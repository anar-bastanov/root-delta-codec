﻿using RdcEngine.Image.Implementations;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace RdcEngine.Image;

internal static class RootDeltaImageTransform
{
    private static readonly Dictionary<ulong, ImageTransformImpl> Implementations = [];

    public static RawImage Encode(RawImage rawImage, ushort version, ushort mode)
    {
        var (_, _, _, channels, _, _) = rawImage;
        var impl = GetImplementation(version, mode, channels);

        return impl.Encode(rawImage);
    }

    public static RawImage Decode(RawImage rawImage, ushort version, ushort mode)
    {
        var (width, height, _, channels, size, _) = rawImage;
        var impl = GetImplementation(version, mode, channels);

        if (impl.ComputeLength(width, height) > size)
            throw new InvalidDataException("RDI size mismatch or incomplete pixel data");

        return impl.Decode(rawImage);
    }
    private static ImageTransformImpl GetImplementation(ushort version, ushort mode, uint channels)
    {
        ImageTransformImpl.Validate(ref version, ref mode, ref channels);

        ulong key = ((ulong)channels << 32) | ((uint)version << 16) | ((uint)mode << 0);
        ref var t = ref CollectionsMarshal.GetValueRefOrAddDefault(Implementations, key, out _);

        return t ??= ImageTransformImpl.Resolve(version, mode, channels);
    }
}
