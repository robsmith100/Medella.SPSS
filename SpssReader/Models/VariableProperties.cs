using System;
using Spss.SpssMetadata;

namespace Spss.Models
{
    public class VariableProperties
    {
        public FormatType FormatType { get; set; }
        public int ValueLength { get; set; }
        public int MissingValueType { get; set; }
        public byte[] ShortName { get; set; } = null!;
        public byte[][]? Missing { get; set; }
        public byte[] Label { get; set; } = Array.Empty<byte>();
        public int Index { get; set; }
        public int SpssWidth { get; set; }
        public byte DecimalPlaces { get; set; }
    }
}