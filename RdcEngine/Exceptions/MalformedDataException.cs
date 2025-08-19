using System;

namespace RdcEngine.Exceptions;

public class MalformedDataException : CodecException
{
    public MalformedDataException()
    {
    }

    public MalformedDataException(string message) : base(message)
    {
    }

    public MalformedDataException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
