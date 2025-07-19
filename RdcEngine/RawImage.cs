namespace RdcEngine;

internal readonly record struct RawImage(
    uint Width,
    uint Height,
    uint Stride,
    uint Channels,
    byte[] Data);
