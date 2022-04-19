using System;
using Spss.SpssMetadata;

namespace Spss.FileStructure;

public readonly struct OutputFormat
{
    public readonly int Value;

    public OutputFormat(FormatType formatType, int fieldWidth, int decimalPlaces = 0)
    {
        var formatBytes = new byte[4];
        formatBytes[0] = (byte)decimalPlaces;
        formatBytes[1] = (byte)fieldWidth;
        formatBytes[2] = (byte)formatType;
        Value = BitConverter.ToInt32(formatBytes, 0);
    }
}