using System;

namespace RdcEngine.Exceptions;

public class MalformedFileException : CodecException
{
    public MalformedFileException()
    {
    }

    public MalformedFileException(string message) : base(message)
    {
    }

    public MalformedFileException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
