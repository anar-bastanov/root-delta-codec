using System;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    public const ushort DefaultMode = 0x0001;

    public abstract RawImage Encode(RawImage rawImage);

    public abstract RawImage Decode(RawImage rawImage);

    public abstract int ComputeLength(int width, int height);

    public static ImageTransformImpl Resolve(ushort mode, int colorSpace)
    {
        if (mode is ushort.MaxValue)
            throw new ArgumentException("Invalid RDI encoding mode", nameof(mode));

        if (colorSpace is not (3 or 4))
            throw new ArgumentException("Invalid number of colorSpace for RDI", nameof(colorSpace));

        var argb = colorSpace is 4;

        ImageTransformImpl impl = (mode, argb) switch
        {
            (0, _)     => Deprecated(),

            (1, false) => new ImageTransform_M1_C3(),
            (1, true)  => new ImageTransform_M1_C4(),

            (2, false) => RgbNotSupported(),
            (2, true)  => RgbaNotSupported(),

            (3, false) => new ImageTransform_M3_C3(),
            (3, true)  => new ImageTransform_M3_C4(),

            _ => throw new NotSupportedException("Unrecognized RDI encoding mode")
        };

        return impl;

        static ImageTransformImpl Deprecated() =>
            throw new NotSupportedException("Deprecated RDI encoding mode");

        static ImageTransformImpl RgbNotSupported() =>
            throw new NotSupportedException("RGB color space is not supported for given RDI encoding mode");

        static ImageTransformImpl RgbaNotSupported() =>
            throw new NotSupportedException("RGBA color space is not supported for given RDI encoding mode");
    }
}
