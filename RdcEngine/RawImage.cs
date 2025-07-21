namespace RdcEngine;

internal record class RawImage(
    uint Width,
    uint Height,
    uint Stride,
    uint Channels,
    byte[] Data);

internal record class RdiRawImage : RawImage
{
    public RdiRawImage(uint Width, uint Height, uint Stride, uint Channels, byte[] Data) : base(Width, Height, Stride, Channels, Data)
    {
    }

    protected RdiRawImage(RawImage original) : base(original)
    {
    }
}

internal record class BmpRawImage : RawImage
{
    public BmpRawImage(uint Width, uint Height, uint Stride, uint Channels, byte[] Data) : base(Width, Height, Stride, Channels, Data)
    {
    }

    protected BmpRawImage(RawImage original) : base(original)
    {
    }
}
