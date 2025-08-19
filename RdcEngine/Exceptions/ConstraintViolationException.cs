using System;

namespace RdcEngine.Exceptions;

public class ConstraintViolationException : CodecException
{
    public ConstraintViolationException()
    {
    }

    public ConstraintViolationException(string message) : base(message)
    {
    }

    public ConstraintViolationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
