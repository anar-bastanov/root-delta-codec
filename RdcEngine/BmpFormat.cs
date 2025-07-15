using System;
using System.IO;

namespace RdcEngine;

internal static class BmpFormat
{
    public static RawImage Load(Stream bmpInput)
    {
        StreamValidator.EnsureReadable(bmpInput);

        throw new NotImplementedException();
    }

    public static void Save(RawImage rawImage, Stream bmpOutput)
    {
        StreamValidator.EnsureWritable(bmpOutput);

        throw new NotImplementedException();
    }
}
