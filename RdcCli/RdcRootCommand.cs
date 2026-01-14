using RdcEngine.Exceptions;
using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RdcCli;

public sealed class RdcRootCommand : RootCommand
{
    private readonly ParserConfiguration _parserConfiguration;

    private readonly InvocationConfiguration _invocationConfiguration;

    private RdcRootCommand() : base()
    {
        _parserConfiguration = new()
        {
            EnablePosixBundling = false
        };

        _invocationConfiguration = new()
        {
            EnableDefaultExceptionHandler = true
        };

        Options[1].Aliases.Add("-v"); // alias for --version option
        Directives.Clear(); // disable suggestion directive
        // cannot disable ugly "Did you mean one of the following?" messages for now...
    }

    public int Invoke(string[] args) =>
        Parse(args, _parserConfiguration).Invoke(_invocationConfiguration);

    public Task<int> InvokeAsync(string[] args) =>
        Parse(args, _parserConfiguration).InvokeAsync(_invocationConfiguration);

    public static RdcRootCommand Build()
    {
        var root = new RdcRootCommand
        {
            Description = "Tool for encoding and decoding Root Delta media family",
            TreatUnmatchedTokensAsErrors = true
        };

        var encodeCommand = new Command("encode")
        {
            Description = "Encode a media file"
        };

        var decodeCommand = new Command("decode")
        {
            Description = "Decode a media file"
        };

        var formatsOption = new Option<(string, string)>("--format", aliases: "-f")
        {
            Description = "Input and output media formats as FROM:TO",
            Arity = ArgumentArity.ExactlyOne,
            CustomParser = result =>
            {
                string value = result.Tokens.Single().Value;
                var extensions = value.Split(':', 2, StringSplitOptions.TrimEntries);

                return (extensions[0], extensions.Length is 2 ? extensions[1] : "");
            }
        };

        var modeOption = new Option<ushort>("--mode", aliases: "-m")
        {
            Description = "Set encoding mode",
            Arity = ArgumentArity.ExactlyOne,
            CustomParser = result =>
            {
                string value = result.Tokens.Single().Value;

                return ushort.TryParse(value, out ushort mode) ? mode : ushort.MaxValue;
            }
        };

        var overwriteOption = new Option<bool>("--overwrite", aliases: "-w")
        {
            Description = "Allow overwriting the output file if it exists",
            Arity = ArgumentArity.Zero
        };

        var inputArg = new Argument<FileInfo>("input")
        {
            Description = "Input file",
            Arity = ArgumentArity.ExactlyOne
        };

        var outputArg = new Argument<FileInfo?>("output")
        {
            Description = "Output file",
            Arity = ArgumentArity.ZeroOrOne
        };

        inputArg.AcceptExistingOnly();
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

        return root;
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
                int exitCode;
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;

                switch (ex)
                {
                    case CommandLineException cliEx:
                        exitCode = cliError;
                        Console.Error.WriteLine($"{ex.Message}.");
                        Console.ForegroundColor = color;

                        if (cliEx.PrintHelp)
                        {
                            Console.Error.WriteLine();
                            root.Parse("--help").Invoke();
                        }
                        break;
                    case CodecException:
                        exitCode = codecError;
                        Console.Error.Write($"Codec error: {ex.Message}.");
                        break;
                    default:
                        exitCode = internalError;
                        Console.Error.Write($"Internal error: {ex.Message}.");
                        break;
                }

                Console.ForegroundColor = color;

#if DEBUG
                Console.Error.WriteLine();
                Console.Error.WriteLine(ex);
#endif

                Environment.Exit(exitCode);
            }
        };
    }
}
