using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RdcCli;

public readonly struct RdcRootCommand(CommandLineConfiguration Command)
{
    public int Invoke(string[] args) => Command.Invoke(args);

    public Task<int> InvokeAsync(string[] args) => Command.InvokeAsync(args);

    public static RdcRootCommand Build()
    {
        var encodeCommand = new Command("encode");
        encodeCommand.Aliases.Add("-e");
        encodeCommand.Aliases.Add("--encode");
        encodeCommand.Description = "Encode a media file";

        var decodeCommand = new Command("decode");
        decodeCommand.Aliases.Add("-d");
        decodeCommand.Aliases.Add("--decode");
        decodeCommand.Description = "Decode a media file";

        var formatsOption = new Option<(string, string)>("--format");
        formatsOption.Aliases.Add("-f");
        formatsOption.Description = "Media formats as FROM:TO";
        formatsOption.CustomParser = result =>
        {
            var value = result.Tokens.Single().Value;
            var extensions = value.Split(':', 2, StringSplitOptions.TrimEntries);
            return (extensions[0], extensions.Length is 2 ? extensions[1] : "");
        };

        var inputArg = new Argument<FileInfo>("input");
        inputArg.Description = "Input file";
        inputArg.AcceptExistingOnly();

        var outputArg = new Argument<FileInfo>("output");
        outputArg.Description = "Output file";
        outputArg.AcceptLegalFilePathsOnly();

        encodeCommand.Add(formatsOption);
        encodeCommand.Add(inputArg);
        encodeCommand.Add(outputArg);

        decodeCommand.Add(formatsOption);
        decodeCommand.Add(inputArg);
        decodeCommand.Add(outputArg);

        var root = new RootCommand();
        root.Description = "Tool for encoding and decoding Root Delta media family";
        root.TreatUnmatchedTokensAsErrors = true;
        root.Add(encodeCommand);
        root.Add(decodeCommand);

        encodeCommand.SetAction(TryCatchBlock(root, parseResult =>
        {
            var formats = parseResult.GetValue(formatsOption);
            var input = parseResult.GetValue(inputArg)!;
            var output = parseResult.GetValue(outputArg)!;

            CommandHandler.RunEncode(formats, input, output);
        }));

        decodeCommand.SetAction(TryCatchBlock(root, parseResult =>
        {
            var formats = parseResult.GetValue(formatsOption);
            var input = parseResult.GetValue(inputArg)!;
            var output = parseResult.GetValue(outputArg)!;

            CommandHandler.RunDecode(formats, input, output);
        }));

        var config = new CommandLineConfiguration(root)
        {
            EnablePosixBundling = false,
            EnableDefaultExceptionHandler = true
        };

        return new(config);
    }

    private static Action<ParseResult> TryCatchBlock(RootCommand root, Action<ParseResult> handler)
    {
        return result =>
        {
            try
            {
                handler(result);
            }
            catch (Exception ex)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;

                if (ex is CommandLineException)
                {
                    Console.Error.WriteLine($"{ex.Message}.\n");
                    Console.ForegroundColor = color;

                    root.Parse("-h").Invoke();
                }
                else
                {
                    Console.Error.Write($"Codec error: {ex.Message}.");
                    Console.ForegroundColor = color;

#if DEBUG
                    Console.Error.WriteLine();
                    Console.Error.WriteLine(ex);
#endif
                }

                Environment.Exit(1);
            }
        };
    }
}
