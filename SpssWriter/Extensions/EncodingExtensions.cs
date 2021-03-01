using System.Text;

namespace Spss.Extensions
{
    public static class EncodingExtensions
    {
        internal static byte[] GetPaddedValueAsByteArray(this Encoding enc, string value, int arraySize)
        {
            var (charLength, _) = GetStringLength(enc, value, arraySize);
            return GetPaddedByteArray(enc, value, charLength, arraySize);
        }

        public static (byte byteLength, byte[] label) GetVariableLabelAsByteArray(this Encoding enc, string value, int maxLength = 252)
        {
            var (charLength, byteLength) = GetStringLength(enc, value, maxLength);

            var arraySize = (byteLength + 3) / 4 * 4;

            return ((byte) byteLength, GetPaddedByteArray(enc, value, charLength, arraySize));
        }

        public static (byte byteLength, byte[] label) GetValueLabelAsByteArray(this Encoding enc, string value, int maxLength = 120)
        {
            var (charLength, byteLength) = GetStringLength(enc, value, maxLength);

            var arraySize = (byteLength + 8) / 8 * 8 - 1;

            return ((byte) byteLength, GetPaddedByteArray(enc, value, charLength, arraySize));
        }

        public static (int charLength, int byteLength) GetStringLength(this Encoding enc, string value, int maxLength)
        {
            int byteLength;
            var charLength = value.Length;
            while ((byteLength = enc.GetByteCount(value, 0, charLength)) > maxLength) charLength--;
            return (charLength, byteLength);
        }

        private static byte[] GetPaddedByteArray(Encoding enc, string value, int charLength, int arraySize)
        {
            var byteArr = new byte[arraySize];
            var length = enc.GetBytes(value, 0, charLength, byteArr, 0);
            for (var i = length; i < byteArr.Length; i++) byteArr[i] = 0x20;
            return byteArr;
        }
    }
}
