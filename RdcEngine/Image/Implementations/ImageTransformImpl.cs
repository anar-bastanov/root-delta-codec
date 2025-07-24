using System;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    private const ushort DefaultMajorVersion = 0x00;

    private const ushort DefaultMinorVersion = 0x01;

    private const ushort DefaultMode = 0x0001;

    public abstract RawImage Encode(RawImage rawImage);

    public abstract RawImage Decode(RawImage rawImage);

    public abstract int ComputeLength(uint width, uint height, uint channels);

    public static ImageTransformImpl Choose(ushort version, ushort mode)
    {
        if ((version & ~0xFF) is 0)
            version |= DefaultMajorVersion << 8;

        if ((version & +0xFF) is 0)
            version |= DefaultMinorVersion << 0;

        if (mode is 0)
            mode = DefaultMode;

        return (version, mode) switch
        {
            (0x00_01, 0x0001) => new ImageTransform__V00_01__M1(),
            (0x00_01, 0x0002) => new ImageTransform__V00_01__M2(),
            _ => throw new NotSupportedException("Unrecognized image encoding mode for the given codec version")
        };
    }
}
