using System.Collections.Generic;
using Spss.FileStructure;
using Spss.SpssMetadata;

namespace Spss.Models
{
    public class VariableWrapper
    {
        private readonly Variable _variable;

        public VariableWrapper(Variable variable)
        {
            _variable = variable;
            OutputFormat = new OutputFormat(variable.FormatType, variable.SpssWidth < 252 ? variable.SpssWidth : 255, variable.DecimalPlaces);
        }

        public OutputFormat OutputFormat { get; }

        public string Name
        {
            get => _variable.Name;
            set => _variable.Name = value;
        }

        public FormatType FormatType => _variable.FormatType;
        public MeasurementType Measure => _variable.MeasurementType;
        public int Columns => _variable.Columns;
        public Alignment Alignment => _variable.Alignment;

        public string? Label
        {
            get => _variable.Label;
            set => _variable.Label = value;
        }

        public MissingValueType MissingValueType => _variable.MissingValueType;
        public object[] MissingValuesObject => _variable.MissingValues;

        internal int ValueLength
        {
            get => _variable.SpssWidth;
            set => _variable.SpssWidth = value;
        }

        internal Dictionary<object, string>? ValueLabels => _variable.ValueLabels;
        internal byte[] ShortName8Bytes { get; set; } = null!;
        internal string ShortName { get; set; } = null!;
        internal List<byte[]> GhostNames { get; } = new();
        internal int LastGhostVariableLength { get; set; }
    }
}