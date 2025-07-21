using System.IO;

namespace RdcEngine;

public static class ImageCodec
{
    public static void Encode(Stream bmpInput, Stream rdiOutput)
    {
        StreamValidator.EnsureReadable(bmpInput);
        StreamValidator.EnsureWritable(rdiOutput);
        StreamValidator.EnsureNotSame(bmpInput, rdiOutput);

        RawImage bmpRawImage = BmpFormat.Load(bmpInput);
        RawImage rdiRawImage = RootDeltaTransform.EncodeImage(bmpRawImage);

        rdiRawImage.Data = RawDataCompressor.Compress(rdiRawImage.Data);
        RdiFormat.Save(rdiRawImage, rdiOutput);
    }

    public static void Decode(Stream rdiInput, Stream bmpOutput)
    {
        StreamValidator.EnsureReadable(rdiInput);
        StreamValidator.EnsureWritable(bmpOutput);
        StreamValidator.EnsureNotSame(rdiInput, bmpOutput);

        RawImage rdiRawImage = RdiFormat.Load(rdiInput);
        rdiRawImage.Data = RawDataCompressor.Decompress(rdiRawImage.Data);

        RawImage bmpRawImage = RootDeltaTransform.DecodeImage(rdiRawImage);
        BmpFormat.Save(bmpRawImage, bmpOutput);
    }
}
