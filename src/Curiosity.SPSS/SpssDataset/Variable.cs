using System;
using System.Collections.Generic;

namespace Curiosity.SPSS.SpssDataset
{
    public class Variable
    {
        private static readonly DateTime Epoc = new(1582, 10, 14, 0, 0, 0, DateTimeKind.Unspecified);
        private string? _name;

        /// <summary>
        ///     Constructs a new Variable object
        /// </summary>
        [Obsolete("Use the constructor with variable name as parameter")]
        public Variable()
        {
            MissingValues = new double[3];
            ValueLabels = new Dictionary<double, string>();
            PrintFormat = new OutputFormat(FormatType.NotUsed1, 0);
            WriteFormat = new OutputFormat(FormatType.NotUsed1, 0);
        }

        /// <summary>
        ///     Constructs a new Variable object
        /// </summary>
        /// <param name="name">The variable name</param>
        /// <exception cref="ArgumentNullException">if name is null</exception>
        public Variable(string? name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            MissingValues = new double[3];
            ValueLabels = new Dictionary<double, string>();
            PrintFormat = new OutputFormat(FormatType.NotUsed1, 0);
            WriteFormat = new OutputFormat(FormatType.NotUsed1, 0);
        }

        /// <summary>
        ///     The measurement type of the variable,  for display purposes
        /// </summary>
        public MeasurementType MeasurementType { get; set; }

        /// <summary>
        ///     The display with
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        ///     Length for strings variables. Expressed in bytes, not chars. Actual char count will depend on the
        ///     encoding used for the data (equal or less than this).
        /// </summary>
        public int TextWidth { get; set; }

        /// <summary>
        ///     The alignment of the variable for display purposes
        /// </summary>
        public Alignment Alignment { get; set; }

        /// <summary>
        ///     Name of the variable
        /// </summary>
        public string Name
        {
            get => _name!;
            set => _name = value ?? throw new ArgumentNullException(nameof(Name));
        }

        /// <summary>
        ///     The variable label
        /// </summary>
        public string? Label { get; set; }

        /// <summary>
        ///     Print format settings
        /// </summary>
        public OutputFormat PrintFormat { get; set; } 

        /// <summary>
        ///     Write format settings
        /// </summary>
        public OutputFormat WriteFormat { get; set; } 

        /// <summary>
        ///     The type of spss data(Numeric/Text)
        /// </summary>
        public DataType Type { get; set; }

        /// <summary>
        ///     Type of custom missing values (besides sysMiss).
        /// </summary>
        public MissingValueType MissingValueType { get; set; }

        /// <summary>
        ///     Holds the value information to be treated as missing values. Depends on the <see cref="MissingValueType" />.
        ///     <para />
        ///     This is a readonly 3 items array.
        /// </summary>
        public double[] MissingValues { get; }

        /// <summary>
        ///     The labels for different values
        /// </summary>
        public IDictionary<double, string>? ValueLabels { get; set; }

        /// <summary>
        ///     The 0-based index of the variable
        /// </summary>
        public int Index { get; internal set; }

        /// <summary>
        ///     Gets the proper value of this variable. This method will check the missing values
        ///     in case there are, and will return null in case the value is one of them.
        ///     Also, if the format fo this variable is a date, it will be transformed into a <see cref="DateTime" />.
        /// </summary>
        /// <param name="value">A value that should be of this variable</param>
        /// <returns>The value as object</returns>
        public object? GetValue(object? value)
        {
            // TODO use strategy pattern to evaluate value (replace MissingValues for strategy impl object)

            if (value == null) return null;

            if (Type != DataType.Numeric)
            {
                var s = value.ToString()?.Trim();
                return s?.Length == 0 ? null : s;
            }

            var cleanValue = MissingValueType == MissingValueType.NoMissingValues ? value : GetWithMissingValueAsNull(value);
            return cleanValue != null && IsDate() ? AsDate(cleanValue) : cleanValue;
        }

        /// <summary>
        ///     Detects whether the current variable is a date, depending on it's write format.
        ///     Time only formats are not considered as a date.
        /// </summary>
        /// <returns></returns>
        public bool IsDate()
        {
            var format = WriteFormat!.FormatType;
            return format == FormatType.ADATE
                   || format == FormatType.DATE
                   || format == FormatType.DATETIME
                   || format == FormatType.EDATE
                   || format == FormatType.JDATE
                   || format == FormatType.SDATE;
        }

        private static DateTime AsDate(object value)
        {
            var dVal = (double) value;
            return Epoc.AddSeconds(dVal);
        }

        /// <summary>
        ///     Gets the numeric value of a date, according to the spss file format
        /// </summary>
        /// <param name="date">The date to transform</param>
        /// <returns>Number of seconds from the 14 of October 1582 to <c>date</c></returns>
        public static double GetValueFromDate(DateTime date)
        {
            var span = date.Subtract(Epoc);
            return span.TotalSeconds;
        }

        private object? GetWithMissingValueAsNull(object value)
        {
            // ReSharper disable CompareOfFloatsByEqualityOperator
            // Comparisons are for exact value, as missing values have to be written in
            var dVal = (double) value;

            switch (MissingValueType)
            {
                case MissingValueType.NoMissingValues:
                    break;
                case MissingValueType.OneDiscreteMissingValue:
                    if (dVal == MissingValues[0])
                        return null;
                    break;
                case MissingValueType.TwoDiscreteMissingValue:
                    if (dVal == MissingValues[0] || dVal == MissingValues[1])
                        return null;
                    break;
                case MissingValueType.ThreeDiscreteMissingValue:
                    if (dVal == MissingValues[0] || dVal == MissingValues[1] || dVal == MissingValues[2])
                        return null;
                    break;
                case MissingValueType.Range:
                    if (dVal >= MissingValues[0] && dVal <= MissingValues[1])
                        return null;
                    break;
                case MissingValueType.RangeAndDiscrete:
                    if (dVal >= MissingValues[0] && dVal <= MissingValues[1]
                        || MissingValueType == MissingValueType.RangeAndDiscrete && dVal == MissingValues[2])
                        return null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"MissingValueType:{MissingValueType}");
            }
            // ReSharper restore CompareOfFloatsByEqualityOperator

            return value;
        }
    }
}