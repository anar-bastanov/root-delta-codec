using System.IO;
using RdcEngine.Image.Formats;

namespace RdcEngine.Image;

public static class ImageCodec
{
    public static void EncodeBmp(Stream bmpInput, Stream rdiOutput,
        ushort mode = 0)
    {
        StreamValidator.EnsureReadable(bmpInput);
        StreamValidator.EnsureWritable(rdiOutput);
        StreamValidator.EnsureNotSame(bmpInput, rdiOutput);

        RawImage bmp = BmpFormat.Load(bmpInput);
        RawImage rdi = RootDeltaImageTransform.Encode(bmp, ref mode);

        RdiFormat.Save(rdi, rdiOutput, mode);
    }

    public static void DecodeBmp(Stream rdiInput, Stream bmpOutput)
    {
        StreamValidator.EnsureReadable(rdiInput);
        StreamValidator.EnsureWritable(bmpOutput);
        StreamValidator.EnsureNotSame(rdiInput, bmpOutput);

        RawImage rdi = RdiFormat.Load(rdiInput, out ushort mode);
        RawImage bmp = RootDeltaImageTransform.Decode(rdi, mode);

        BmpFormat.Save(bmp, bmpOutput);
    }
}
