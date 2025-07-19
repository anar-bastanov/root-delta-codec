using System;

namespace RdcEngine;

internal static class RootDeltaTransform
{
    public static RawImage EncodeImage(RawImage rawImage)
    {
        return rawImage with { Data = rawImage.Data[..] };
    }

    public static RawImage DecodeImage(RawImage rawImage)
    {
        return rawImage with { Data = rawImage.Data[..] };
    }
}
