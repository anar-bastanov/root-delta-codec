using System;

namespace RdcCli;

public sealed class CommandLineException : Exception
{
    public bool PrintHelp { get; init; }

    public CommandLineException()
    {
    }

    public CommandLineException(string message, bool printHelp = false) : base(message)
    {
        PrintHelp = printHelp;
    }

    public CommandLineException(string? message, Exception? innerException, bool printHelp = false) : base(message, innerException)
    {
        PrintHelp = printHelp;
    }
}
