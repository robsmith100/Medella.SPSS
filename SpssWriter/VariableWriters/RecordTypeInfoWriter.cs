using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Spss.Extensions;
using Spss.Models;
using SpssCommon.Models;
using SpssCommon.VariableModel;

namespace Spss.VariableWriters
{
    public class RecordTypeInfoWriter
    {
        private readonly Encoding _encoding;
        private readonly BinaryWriter _writer;

        public RecordTypeInfoWriter(BinaryWriter writer, Encoding encoding)
        {
            _writer = writer;
            _encoding = encoding;
        }

        public void WriteMachineIntegerInfoRecord()
        {
            WriteInfoHeader(InfoRecordType.MachineInteger, 4, MachineIntegerInfo.Items.Count);
            MachineIntegerInfo.Items.ForEach(x => _writer.Write(x));
        }

        public void WriteMachineFloatingPointInfoRecord()
        {
            WriteInfoHeader(InfoRecordType.MachineFloatingPoint, 8, MachineFloatingPointInfo.Items.Count);
            MachineFloatingPointInfo.Items.ForEach(x => _writer.Write(x));
        }

        public void WriteDisplayValuesInfoRecord(List<DisplayParameter> variables)
        {
            WriteInfoHeader(InfoRecordType.VariableDisplayParameter, 4, variables.Count * 3);
            foreach (var displayValue in variables)
            {
                _writer.Write((int)displayValue.Measure);
                _writer.Write(displayValue.Columns);
                _writer.Write((int)displayValue.Alignment);
            }
        }

        public void WriteLongVariableNamesRecord(List<VariableWrapper> variables)
        {
            var str = string.Join('\t', variables.Select(x => $"{x.ShortName}={x.Name}"));
            var bytes = _encoding.GetBytes(str);
            WriteInfoHeader(InfoRecordType.LongVariableNames, 1, bytes.Length);
            _writer.Write(bytes);
        }

        public void WriteVeryLongStringRecord(List<VariableWrapper> variables)
        {
            var str = string.Join("", variables.Where(x => x.ValueLength > 255).Select(x => $"{x.ShortName}={x.ValueLength}\0\t"));
            if (str.Length == 0) return;

            var bytes = _encoding.GetBytes(str);
            WriteInfoHeader(InfoRecordType.ValueLengthVeryLongString, 1, bytes.Length);
            _writer.Write(bytes);
        }

        public void WriteCharacterEncodingRecord()
        {
            WriteInfoHeader(InfoRecordType.CharacterEncoding, 1, _encoding.WebName.Length);
            _writer.Write(_encoding.GetBytes(_encoding.WebName.ToUpper()));
        }

        private void WriteInfoHeader(int subtype, int itemSize, int itemCount)
        {
            _writer.Write((int)RecordType.InfoRecord);
            _writer.Write(subtype);
            _writer.Write(itemSize);
            _writer.Write(itemCount);
        }

        public void WriteValueLabelStringRecords(List<VariableWrapper> records)
        {
            foreach (var longStringValueLabels in records.Where(x => x.FormatType == FormatType.A && x.ValueLabels != null && x.ValueLabels.Any() && x.ValueLength > 8))
                WriteValueLabelStringRecord(longStringValueLabels);
        }

        private void WriteValueLabelStringRecord(VariableWrapper variable)
        {
            var variableName = _encoding.GetBytes(variable.Name!) ;
            var variableLength = variableName.Length;
            var valueLength = variable.ValueLength;

            var valueLabels = variable.ValueLabels!.ToDictionary(x => _encoding.GetPaddedValueAsByteArray((string) x.Key, valueLength),x=>_encoding.GetBytes(x.Value));
            var valuesLength = valueLabels!.Select(x => 4 + valueLength + 4 + x.Value.Length).Aggregate((a, b) => a + b);
            var length = 4 + variableLength + 4 + 4 + valuesLength;
            WriteInfoHeader(InfoRecordType.LongStringValueLabels, 1, length);
            _writer.Write(variableLength);
            _writer.Write(variableName);
            _writer.Write(valueLength);
            _writer.Write(variable.ValueLabels!.Count);
            foreach (var (value, label) in valueLabels)
            {
                // Write the value of the value label
                _writer.Write(valueLength);
                _writer.Write(value);
                //Write the label bytes
                _writer.Write(label.Length);
                _writer.Write(label);
            }
        }

        public void WriteMissingStringRecords(List<VariableWrapper> records)
        {
            foreach (var nonStringValueLabels in records.Where(x => x.FormatType == FormatType.A && x.MissingValueType != MissingValueType.NoMissingValues && x.ValueLength > 8))
                WriteMissingStringRecord(nonStringValueLabels);
        }

        private void WriteMissingStringRecord(VariableWrapper variable)
        {
            var variableName = _encoding.GetBytes(variable.Name!);
            var variableLength = variableName.Length;
            var missingCount = Math.Abs((int)variable.MissingValueType);
            var length = 4 + variableLength + 1 + 4 + 8 * missingCount;
            WriteInfoHeader(InfoRecordType.LongStringMissing, 1, length);
            _writer.Write(variableLength);
            _writer.Write(variableName); //padding with 0x0A
            _writer.Write((byte)missingCount);
            _writer.Write(8);
            foreach (var missing in variable.MissingValuesObject)
                _writer.Write(_encoding.GetPaddedValueAsByteArray((string)missing, 8));
        }
    }
}
