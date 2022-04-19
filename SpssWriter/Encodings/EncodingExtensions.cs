using System.Text;

namespace Spss.Encodings;

public static class EncodingExtensions
{
    internal static byte[] GetPaddedValueAsByteArray(this Encoding enc, string value, int arraySize)
    {
        value = GetTrimmedString(enc, value, arraySize).Value;
        return GetPaddedByteArray(enc, value, arraySize);
    }

    public static (byte ByteLength, byte[] Label) GetVariableLabelAsByteArray(this Encoding enc, string value, int maxLength = 252)
    {
        var (valueNew, byteLength) = GetTrimmedString(enc, value, maxLength);

        var arraySize = (byteLength + 3) / 4 * 4;

        return ((byte)byteLength, GetPaddedByteArray(enc, valueNew, arraySize));
    }

    public static (byte ByteLength, byte[] Label) GetValueLabelAsByteArray(this Encoding enc, string value, int maxLength = 120)
    {
        var (valueNew, byteLength) = GetTrimmedString(enc, value, maxLength);

        var arraySize = (byteLength + 8) / 8 * 8 - 1;

        return ((byte)byteLength, GetPaddedByteArray(enc, valueNew, arraySize));
    }

    public static byte[] GetBytes(this Encoding enc, string value, int maxLength)
    {
        value = GetTrimmedString(enc, value, maxLength).Value;

        return enc.GetBytes(value);
    }

    public static string GetString(this Encoding enc, string value, int maxLength)
    {
        return enc.GetString(enc.GetBytes(value, maxLength));
    }

    public static (string Value, int ByteLength) GetTrimmedString(this Encoding enc, string value, int maxLength)
    {
        int byteLength;
        if (value.Length > maxLength) value = value[..maxLength];

        while ((byteLength = enc.GetByteCount(value)) > maxLength)
        {
            var x = (byteLength - maxLength + 3) / 4;
            value = value[..^x];
        }

        return (value, byteLength);
    }

    private static byte[] GetPaddedByteArray(Encoding enc, string value, int arraySize)
    {
        var byteArr = new byte[arraySize];
        var length = enc.GetBytes(value, 0, value.Length, byteArr, 0);
        for (var i = length; i < byteArr.Length; i++) byteArr[i] = 0x20;

        return byteArr;
    }
}