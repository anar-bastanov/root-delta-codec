namespace RdcEngine.Image;

internal record struct RawImage(
    int Width,
    int Height,
    int Stride,
    int ColorSpace,
    int Size,
    byte[] Data);
