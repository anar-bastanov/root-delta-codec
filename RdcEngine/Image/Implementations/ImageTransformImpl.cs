using System;

namespace RdcEngine.Image.Implementations;

internal abstract partial class ImageTransformImpl
{
    private const ushort DefaultMajorVersion = 0x01;

    private const ushort DefaultMinorVersion = 0x01;

    private const ushort DefaultMode = 0x0001;

    public abstract RawImage Encode(RawImage rawImage);

    public abstract RawImage Decode(RawImage rawImage);

    public abstract int ComputeLength(uint width, uint height);

    public static void Validate(ref ushort version, ref ushort mode, ref uint channels)
    {
        byte major = (byte)(version >> 8), minor = (byte)(version >> 0);

        if (major is byte.MaxValue || minor is byte.MaxValue)
            throw new ArgumentException("Invalid RDC version number", nameof(version));

        if (mode is ushort.MaxValue)
            throw new ArgumentException("Invalid RDC encoding mode", nameof(mode));

        if (channels is not 3 and not 4)
            throw new ArgumentException("Invalid number of channels in RDI", nameof(channels));

        if (major is 0)
            version |= DefaultMajorVersion << 8;

        if (minor is 0)
            version |= DefaultMinorVersion << 0;

        if (mode is 0)
            mode = DefaultMode;
    }

    public static ImageTransformImpl Resolve(ushort version, ushort mode, uint channels)
    {
        return (version, mode, channels) switch
        {
            (0x01_01, 0x0001, 3) => new ImageTransform__V01_01__M1__C3(),
            (0x01_01, 0x0001, 4) => new ImageTransform__V01_01__M1__C4(),
            _ => throw new NotSupportedException("Unrecognized image encoding mode for the given codec version")
        };
    }
}
