namespace RdcEngine.Image;

internal record struct RawImage(
    int Width,
    int Height,
    int Stride,
    int ColorModel,
    int Size,
    byte[] Data);
