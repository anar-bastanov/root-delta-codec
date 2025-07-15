using System;
using System.IO;

namespace RdcEngine;

internal static class StreamValidator
{
    public static void EnsureReadable(Stream stream, bool requireSeek = true)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
            throw new ArgumentException("Stream is not readable");

        if (requireSeek && !stream.CanSeek)
            throw new NotSupportedException("Stream must support seeking");
    }

    public static void EnsureWritable(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanWrite)
            throw new ArgumentException("Stream is not writable");
    }

    public static void EnsureNotSame(Stream stream1, Stream stream2)
    {
        ArgumentNullException.ThrowIfNull(stream1);
        ArgumentNullException.ThrowIfNull(stream2);

        if (ReferenceEquals(stream1, stream2))
            throw new ArgumentException("Input and output streams must not be the same");
    }

    public static void EnsureRemaining(Stream stream, long neededBytes)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanSeek)
            throw new NotSupportedException("Cannot check length on non-seekable stream");

        if (stream.Length - stream.Position < neededBytes)
            throw new InvalidDataException($"Stream does not contain the required {neededBytes} bytes");
    }
}
