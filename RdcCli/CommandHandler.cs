using RdcEngine.Image;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using static RdcCli.MediaFormat;

namespace RdcCli;

public static class CommandHandler
{
    private static void SafeOverwrite(
        Action<(MediaFormat From, MediaFormat To), FileStream, FileStream> handler,
        (string From, string To) userExtensions, FileInfo input, FileInfo? output,
        bool overwrite)
    {
        var tempFile = new FileInfo(Path.Combine(output?.DirectoryName ?? Path.GetTempPath(), Path.GetRandomFileName()));

        try
        {
            var formats = InferFileArguments(userExtensions, input, ref output);

            if (!overwrite && output.Exists)
                throw new CommandLineException($"File '{output.Name}' already exists. Use --overwrite to allow replacing it");

            using (var tempStream = tempFile.Create())
                using (var inputStream = input.OpenRead())
                    handler(formats, inputStream, tempStream);

            tempFile.MoveTo(output.FullName, true);
        }
        catch
        {
            tempFile.Delete();
            throw;
        }
    }

    public static void RunEncode(
        (string From, string To) userExtensions, FileInfo input, FileInfo? output,
        ushort version, ushort mode, bool overwrite)
    {
        SafeOverwrite((e, i, o) => RunEncodeInternal(e, i, o, version, mode),
            userExtensions, input, output, overwrite);
    }

    public static void RunDecode(
        (string From, string To) userExtensions, FileInfo input, FileInfo? output,
        bool overwrite)
    {
        SafeOverwrite(RunDecodeInternal,
            userExtensions, input, output, overwrite);
    }

    public static void RunEncodeInternal(
        (MediaFormat From, MediaFormat To) mediaFormats, FileStream inputStream, FileStream outputStream,
        ushort version, ushort mode)
    {
        switch (mediaFormats)
        {
            case (BmpImage, RdiImage):
                ImageCodec.EncodeBmp(inputStream, outputStream, version, mode);
                break;
            case (PngImage, RdiImage):
            case (JpegImage, RdiImage):
                throw new NotImplementedException("Image format not implemented");

            case (GifAnimatedImage, RdaiAnimatedImage):
            case (WebpAnimatedImage, RdaiAnimatedImage):
                throw new NotImplementedException("Animated image format not implemented");

            case (Mp4Video, RdvVideo):
            case (AviVideo, RdvVideo):
            case (MovVideo, RdvVideo):
            case (WebmVideo, RdvVideo):
                throw new NotImplementedException("Video format not implemented");

            case (Mp3Audio, RdaAudio):
            case (WavAudio, RdaAudio):
                throw new NotImplementedException("Audio format not implemented");

            case (_, RdiImage):
            case (_, RdaiAnimatedImage):
            case (_, RdvVideo):
            case (_, RdaAudio):
                throw new ArgumentException("Conversion not supported");

            case (_, UnknownMedia):
                throw new NotSupportedException("Unknown RDC media format");

            default:
                throw new ArgumentException("Not an RDC media format");
        }
    }

    public static void RunDecodeInternal(
        (MediaFormat From, MediaFormat To) mediaFormats, FileStream inputStream, FileStream outputStream)
    {
        switch (mediaFormats)
        {
            case (RdiImage, BmpImage):
                ImageCodec.DecodeBmp(inputStream, outputStream);
                break;
            case (RdiImage, PngImage):
            case (RdiImage, JpegImage):
                throw new NotImplementedException("Image format not implemented");

            case (RdaiAnimatedImage, GifAnimatedImage):
            case (RdaiAnimatedImage, WebpAnimatedImage):
                throw new NotImplementedException("Animated image format not implemented");

            case (RdvVideo, Mp4Video):
            case (RdvVideo, AviVideo):
            case (RdvVideo, MovVideo):
            case (RdvVideo, WebmVideo):
                throw new NotImplementedException("Video format not implemented");

            case (RdaAudio, Mp3Audio):
            case (RdaAudio, WavAudio):
                throw new NotImplementedException("Audio format not implemented");

            case (RdiImage, _):
            case (RdaiAnimatedImage, _):
            case (RdvVideo, _):
            case (RdaAudio, _):
                throw new ArgumentException("Conversion not supported");

            case (UnknownMedia, _):
                throw new NotSupportedException("Unknown RDC media format");

            default:
                throw new ArgumentException("Not an RDC media format");
        }
    }

    private static (MediaFormat From, MediaFormat To) InferFileArguments(
        (string From, string To) userExtensions, FileInfo input, [NotNull] ref FileInfo? output)
    {
        output ??= new FileInfo(Path.ChangeExtension(input.FullName, userExtensions.To));

        var inputExtension = input.Extension;
        var outputExtension = output.Extension;

        if (userExtensions.From is var from and not (null or ""))
            inputExtension = from;

        if (userExtensions.To is var to and not (null or ""))
            outputExtension = to;

        if (inputExtension is "" || outputExtension is "")
            throw new CommandLineException("Cannot infer media formats. Specify --format or use files with explicit extensions");

        return (ParseMediaExtension(inputExtension), ParseMediaExtension(outputExtension));
    }

    private static MediaFormat ParseMediaExtension(string extension)
    {
        return extension.TrimStart('.').ToLowerInvariant() switch
        {
            "rdi" => RdiImage,
            "bmp" => BmpImage,
            "png" => PngImage,
            "jpg" => JpegImage,
            "jpeg" => JpegImage,
            "rdai" => RdaiAnimatedImage,
            "gif" => GifAnimatedImage,
            "webp" => WebpAnimatedImage,
            "rdv" => RdvVideo,
            "mp4" => Mp4Video,
            "avi" => AviVideo,
            "mov" => MovVideo,
            "webm" => WebmVideo,
            "rda" => RdaAudio,
            "mp3" => Mp3Audio,
            "wav" => WavAudio,
            _ => UnknownMedia
        };
    }
}
