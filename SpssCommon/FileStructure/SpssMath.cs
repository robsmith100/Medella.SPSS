using System;

namespace Spss.FileStructure;

public static class SpssMath
{
    private static readonly DateTime Epoch = new(1582, 10, 14, 0, 0, 0, DateTimeKind.Unspecified);

    public static int GetNumberOfGhostVariables(int valueLength)
    {
        return valueLength < 256 ? 0 : (valueLength - 1) / 252;
    }

    public static int GetAllocatedSize(int valueLength)
    {
        return GetNumberOf32ByteBlocks(valueLength) * 8;
    }

    public static int GetNumberOf32ByteBlocks(int valueLength)
    {
        var filledBlocks = GetNumberOf256ByteBlocks(valueLength) - 1;
        return (valueLength - filledBlocks * 252 + 7) / 8 + filledBlocks * 32;
    }

    public static int GetNumberOf256ByteBlocks(int valueLength)
    {
        return valueLength < 256 ? 1 : (valueLength - 1) / 252 + 1;
    }

    public static double SpssDate(this DateTime date)
    {
        return date.Subtract(Epoch).TotalSeconds;
    }

    public static DateTime AsDate(this double value)
    {
        return Epoch.AddSeconds(value);
    }
}