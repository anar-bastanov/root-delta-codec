using System;
using System.IO;
using RdcEngine;

if (args.Length is not 2)
{
    Environment.Exit(1);
}

string inputBmp = args[0];
string outputRdi = args[1];

try
{
    using var input = File.OpenRead(inputBmp);
    using var output = File.Create(outputRdi);

    ImageCodec.Encode(input, output);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}
