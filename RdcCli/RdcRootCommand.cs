using RdcEngine;
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
        formatsOption.Description = "Input and output media formats as FROM:TO";
        formatsOption.Arity = ArgumentArity.ExactlyOne;
        formatsOption.CustomParser = result =>
        {
            string value = result.Tokens.Single().Value;
            var extensions = value.Split(':', 2, StringSplitOptions.TrimEntries);

            return (extensions[0], extensions.Length is 2 ? extensions[1] : "");
        };

        var modeOption = new Option<ushort>("--mode");
        modeOption.Aliases.Add("-m");
        modeOption.Description = "Set encoding mode";
        modeOption.Arity = ArgumentArity.ExactlyOne;
        modeOption.CustomParser = result =>
        {
            string value = result.Tokens.Single().Value;

            return ushort.TryParse(value, out ushort mode) ? mode : ushort.MaxValue;
        };

        var overwriteOption = new Option<bool>("--overwrite");
        overwriteOption.Aliases.Add("-w");
        overwriteOption.Description = "Allow overwriting the output file if it exists";
        overwriteOption.Arity = ArgumentArity.Zero;

        var inputArg = new Argument<FileInfo>("input");
        inputArg.Description = "Input file";
        inputArg.Arity = ArgumentArity.ExactlyOne;
        inputArg.AcceptExistingOnly();

        var outputArg = new Argument<FileInfo?>("output");
        outputArg.Description = "Output file";
        outputArg.Arity = ArgumentArity.ZeroOrOne;
        outputArg.AcceptLegalFilePathsOnly();

        encodeCommand.Add(formatsOption);
        encodeCommand.Add(modeOption);
        encodeCommand.Add(overwriteOption);
        encodeCommand.Add(inputArg);
        encodeCommand.Add(outputArg);

        decodeCommand.Add(formatsOption);
        decodeCommand.Add(overwriteOption);
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
            var output = parseResult.GetValue(outputArg);
            ushort mode = parseResult.GetValue(modeOption);
            bool overwrite = parseResult.GetValue(overwriteOption);

            CommandHandler.RunEncode(formats, input, output, mode, overwrite);
        }));

        decodeCommand.SetAction(TryCatchBlock(root, parseResult =>
        {
            var formats = parseResult.GetValue(formatsOption);
            var input = parseResult.GetValue(inputArg)!;
            var output = parseResult.GetValue(outputArg);
            bool overwrite = parseResult.GetValue(overwriteOption);

            CommandHandler.RunDecode(formats, input, output, overwrite);
        }));

        var config = new CommandLineConfiguration(root);
        config.EnablePosixBundling = false;
        config.EnableDefaultExceptionHandler = true;

        return new(config);
    }

    private static Action<ParseResult> TryCatchBlock(RootCommand root, Action<ParseResult> handler)
    {
        const int cliError = 1;
        const int codecError = 2;
        const int internalError = 3;

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
                    Environment.Exit(cliError);
                }

                bool isCodecError = ex is CodecException;

                if (isCodecError)
                    Console.Error.Write($"Codec error: {ex.Message}.");
                else
                    Console.Error.Write($"Internal error: {ex.Message}.");

                Console.ForegroundColor = color;

#if DEBUG
                Console.Error.WriteLine();
                Console.Error.WriteLine(ex);
#endif

                Environment.Exit(isCodecError ? codecError : internalError);
            }
        };
    }
}
