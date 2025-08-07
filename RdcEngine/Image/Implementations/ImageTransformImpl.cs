namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    public const ushort DefaultMode = 0x0003;

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
            (0, _)     => Deprecated(),

            (1, false) => new ImageTransform_M1_C3(),
            (1, true)  => new ImageTransform_M1_C4(),

            (2, false) => RgbNotSupported(),
            (2, true)  => RgbaNotSupported(),

            (3, false) => new ImageTransform_M3_C3(),
            (3, true)  => new ImageTransform_M3_C4(),

            (4, false) => new ImageTransform_M4_C3(),
            (4, true)  => new ImageTransform_M4_C4(),

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
