using RdcEngine.Exceptions;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace RdcEngine;

internal static class StreamValidator
{
    public static void EnsureReadable(Stream stream, bool requireSeek = true,
        [CallerArgumentExpression(nameof(stream))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(stream, paramName);

        if (!stream.CanRead)
            throw new CodecException("Stream is not readable");

        if (requireSeek && !stream.CanSeek)
            throw new CodecException("Stream must support seeking");
    }

    public static void EnsureWritable(Stream stream,
        [CallerArgumentExpression(nameof(stream))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(stream, paramName);

        if (!stream.CanWrite)
            throw new CodecException("Stream is not writable");
    }

    public static void EnsureNotSame(Stream stream1, Stream stream2,
        [CallerArgumentExpression(nameof(stream1))] string? paramName1 = null,
        [CallerArgumentExpression(nameof(stream2))] string? paramName2 = null)
    {
        ArgumentNullException.ThrowIfNull(stream1, paramName1);
        ArgumentNullException.ThrowIfNull(stream2, paramName2);

        if (ReferenceEquals(stream1, stream2))
            throw new CodecException("Input and output streams must not be the same");
    }

    public static void EnsureRemaining(Stream stream, ulong neededBytes,
        [CallerArgumentExpression(nameof(stream))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(stream, paramName);

        if (!stream.CanSeek)
            throw new CodecException("Cannot check length on non-seekable stream");

        if ((ulong)(stream.Length - stream.Position) < neededBytes)
            throw new CodecException($"Stream does not contain the required {neededBytes} bytes");
    }
}
