using System;

namespace RdcEngine.Exceptions;

public class VariantNotSupportedException : CodecException
{
    public VariantNotSupportedException()
    {
    }

    public VariantNotSupportedException(string message) : base(message)
    {
    }

    public VariantNotSupportedException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
