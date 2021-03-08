using System;

namespace Spss.Models
{
    public static class MetadataInfoExtensions
    {
        public static int EnsureEndianSupport(this MetadataInfo metadataInfo, int i)
        {
            if (metadataInfo.IsLittleEndian) return i;
            var bytes = BitConverter.GetBytes(i);
            Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public static double ConvertDouble(this MetadataInfo metadataInfo, byte[] bytes)
        {
            if (metadataInfo.IsLittleEndian) return BitConverter.ToDouble(bytes);
            Array.Reverse(bytes);
            return BitConverter.ToDouble(bytes, 0);
        }

        public static byte[] ConvertDouble(this MetadataInfo metadataInfo, double d)
        {
            var bytes = BitConverter.GetBytes(d);
            if (!metadataInfo.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }
    }
}