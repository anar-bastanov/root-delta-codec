namespace RdcEngine;

internal readonly record struct RawImage(
    int Width,
    int Height,
    int Stride,
    int Channels,
    byte[] Data);
