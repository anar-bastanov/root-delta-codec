namespace RdcEngine.Image;

internal record struct RawImage(
    uint Width,
    uint Height,
    uint Stride,
    uint Channels,
    uint Size,
    byte[] Data);
