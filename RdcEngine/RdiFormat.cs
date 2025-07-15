using System;
using System.IO;

namespace RdcEngine;

internal static class RdiFormat
{
    public static RawImage Load(Stream rdiInput)
    {
        StreamValidator.EnsureReadable(rdiInput);

        throw new NotImplementedException();
    }

    public static void Save(RawImage rawImage, Stream rdiOutput)
    {
        StreamValidator.EnsureWritable(rdiOutput);

        throw new NotImplementedException();
    }
}
