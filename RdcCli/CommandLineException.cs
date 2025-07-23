using System;

namespace RdcCli;

public sealed class CommandLineException : Exception
{
    public CommandLineException()
    {
    }

    public CommandLineException(string message) : base(message)
    {
    }

    public CommandLineException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
