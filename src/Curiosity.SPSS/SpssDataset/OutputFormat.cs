using System;

namespace Curiosity.SPSS.SpssDataset
{
    /// <summary>
    ///     Specifies a write/print format
    /// </summary>
    public class OutputFormat
    {
        /// <summary>
        ///     Creates a write/print format specification
        /// </summary>
        /// <param name="formatType"></param>
        /// <param name="fieldWidth"></param>
        /// <param name="decimalPlaces"></param>
        public OutputFormat(FormatType formatType, int fieldWidth, int decimalPlaces = 0)
        {
            DecimalPlaces = decimalPlaces;
            FieldWidth = fieldWidth;
            FormatType = formatType;
        }

        internal OutputFormat(int formatValue)
        {
            var formatBytes = BitConverter.GetBytes(formatValue);
            DecimalPlaces = formatBytes[0];
            FieldWidth = formatBytes[1];
            FormatType = (FormatType) formatBytes[2];
        }

        /// <summary>
        ///     Number of decimal places
        /// </summary>
        public int DecimalPlaces { get; }

        /// <summary>
        ///     The display width of the filed
        /// </summary>
        public int FieldWidth { get; }

        /// <summary>
        ///     The format type
        /// </summary>
        public FormatType FormatType { get; }

        internal int GetInteger()
        {
            var formatBytes = new byte[4];
            formatBytes[0] = (byte) DecimalPlaces;
            formatBytes[1] = (byte) FieldWidth;
            formatBytes[2] = (byte) FormatType;

            return BitConverter.ToInt32(formatBytes, 0);
        }
    }
}