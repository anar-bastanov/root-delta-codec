using System.IO;
using RdcEngine.Image.Formats;

namespace RdcEngine.Image;

public static class ImageCodec
{
    public static void EncodeBmp(Stream bmpInput, Stream rdiOutput,
        ushort version = 0, ushort mode = 0)
    {
        StreamValidator.EnsureReadable(bmpInput);
        StreamValidator.EnsureWritable(rdiOutput);
        StreamValidator.EnsureNotSame(bmpInput, rdiOutput);

        RawImage bmpRawImage = BmpFormat.Load(bmpInput);
        RawImage rdiRawImage = RootDeltaImageTransform.Encode(bmpRawImage, version, mode);

        rdiRawImage.Data = RawDataCompressor.Compress(rdiRawImage.Data);
        RdiFormat.Save(rdiRawImage, rdiOutput, version, mode);
    }

    public static void DecodeBmp(Stream rdiInput, Stream bmpOutput)
    {
        StreamValidator.EnsureReadable(rdiInput);
        StreamValidator.EnsureWritable(bmpOutput);
        StreamValidator.EnsureNotSame(rdiInput, bmpOutput);

        RawImage rdiRawImage = RdiFormat.Load(rdiInput, out ushort version, out ushort mode);
        rdiRawImage.Data = RawDataCompressor.Decompress(rdiRawImage.Data);

        RawImage bmpRawImage = RootDeltaImageTransform.Decode(rdiRawImage, version, mode);
        BmpFormat.Save(bmpRawImage, bmpOutput);
    }
}
