using RdcEngine.Exceptions;
using static RdcEngine.Image.ColorSpace;

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

        if (colorSpace is not (Gray or Rgb or Rgba))
            throw new VariantNotSupportedException("Invalid color space for RDI");

        ImageTransformImpl impl = (mode, colorSpace) switch
        {
            (1, _) => Deprecated(),

            // example usage
            (2, Gray) => RgbNotSupported(),
            (2, Rgb)  => RgbNotSupported(),
            (2, Rgba) => RgbaNotSupported(),

            (3, _) => Deprecated(),

            (4, _) => Deprecated(),

            (5, Gray) => new ImageTransform_M5_C1(),
            (5, Rgb)  => new ImageTransform_M5_C3(),
            (5, Rgba) => new ImageTransform_M5_C4(),

            (6, Gray) => GrayNotSupported(),
            (6, Rgb)  => new ImageTransform_M6_C3(),
            (6, Rgba) => new ImageTransform_M6_C4(),

            (7, _) => Deprecated(),

            (8, Gray) => new ImageTransform_M8_C1(),
            (8, Rgb)  => new ImageTransform_M8_C3(),
            (8, Rgba) => new ImageTransform_M8_C4(),

            (9, Gray) => GrayNotSupported(),
            (9, Rgb)  => new ImageTransform_M9_C3(),
            (9, Rgba) => new ImageTransform_M9_C4(),

            (10, _) => Deprecated(),

            _ => throw new VariantNotSupportedException("Unrecognized RDI encoding mode")
        };

        return impl;

        static ImageTransformImpl Deprecated() =>
            throw new VariantNotSupportedException("Deprecated RDI encoding mode");

        static ImageTransformImpl GrayNotSupported() =>
            throw new VariantNotSupportedException("Gray color space is not supported for given RDI encoding mode");

        static ImageTransformImpl RgbNotSupported() =>
            throw new VariantNotSupportedException("RGB color space is not supported for given RDI encoding mode");

        static ImageTransformImpl RgbaNotSupported() =>
            throw new VariantNotSupportedException("RGBA color space is not supported for given RDI encoding mode");
    }
}
