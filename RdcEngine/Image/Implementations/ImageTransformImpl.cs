namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    public const ushort DefaultMode = 0x0005;

    public abstract RawImage Encode(RawImage rawImage);

    public abstract RawImage Decode(RawImage rawImage);

    public abstract int ComputeLength(int width, int height);

    public static ImageTransformImpl Resolve(ushort mode, int colorSpace)
    {
        if (mode is ushort.MaxValue)
            throw new CodecException("Invalid RDI encoding mode");

        if (colorSpace is not (3 or 4))
            throw new CodecException("Invalid color space for RDI");

        var argb = colorSpace is 4;

        ImageTransformImpl impl = (mode, argb) switch
        {
            (1, _)     => Deprecated(),

            // example usage
            (2, false) => RgbNotSupported(),
            (2, true)  => RgbaNotSupported(),

            (3, _)     => Deprecated(),

            (4, _)     => Deprecated(),

            (5, false) => new ImageTransform_M5_C3(),
            (5, true)  => new ImageTransform_M5_C4(),

            (6, false) => new ImageTransform_M6_C3(),
            (6, true)  => new ImageTransform_M6_C4(),

            (7, _)     => Deprecated(),

            (8, false) => new ImageTransform_M8_C3(),
            (8, true)  => new ImageTransform_M8_C4(),

            (9, false) => new ImageTransform_M9_C3(),
            (9, true)  => new ImageTransform_M9_C4(),

            (10, _)    => Deprecated(),

            _ => throw new CodecException("Unrecognized RDI encoding mode")
        };

        return impl;

        static ImageTransformImpl Deprecated() =>
            throw new CodecException("Deprecated RDI encoding mode");

        static ImageTransformImpl RgbNotSupported() =>
            throw new CodecException("RGB color space is not supported for given RDI encoding mode");

        static ImageTransformImpl RgbaNotSupported() =>
            throw new CodecException("RGBA color space is not supported for given RDI encoding mode");
    }
}
