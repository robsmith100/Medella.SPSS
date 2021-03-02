using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Spss.Encodings;
using Spss.Models;
using SpssCommon.FileStructure;
using SpssCommon.SpssMetadata;

namespace Spss.MetadataWriters.RecordWriters
{
    public class RecordTypeWriter
    {
        private static readonly OutputFormat BlankOutputFormat = new(FormatType.A, 0x1d, 1);
        private static readonly byte[] BlankShortName = { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
        private readonly Encoding _encoding;
        private readonly DateTimeFormatInfo _invariantCultureDateTimeFormat = CultureInfo.InvariantCulture.DateTimeFormat;
        private readonly BinaryWriter _writer;


        public RecordTypeWriter(BinaryWriter writer, Encoding encoding)
        {
            _writer = writer;
            _encoding = encoding;
        }

        public void WriteVariableRecords(List<VariableWrapper> records)
        {
            foreach (var record in records)
                WriteVariableRecord(record);
        }

        public void WriteHeaderRecord(Metadata metadata, List<VariableWrapper> variablesCount)
        {
            var count = variablesCount.Select(x => SpssMath.GetNumberOf32ByteBlocks(x.ValueLength)).Aggregate((result, blocks) => result + blocks);
            _writer.Write((int) RecordType.HeaderRecord);
            _writer.Write(_encoding.GetPaddedValueAsByteArray("@(#) IBM SPSS STATISTICS MS Windows 25.0.0.0", 60));
            _writer.Write(2); //LayoutCode
            _writer.Write(count);
            _writer.Write(1); //compressed
            _writer.Write(0); //WeightIndex
            _writer.Write(metadata.Cases);
            _writer.Write((double) metadata.Bias);
            _writer.Write(DateTime.Now.ToString("dd MMM yyHH:mm:ss", _invariantCultureDateTimeFormat).ToCharArray());
            _writer.Write(_encoding.GetPaddedValueAsByteArray(string.Empty, 64));
            _writer.Write(new byte[3]);
        }


        public void WriteDictionaryTerminationRecord()
        {
            _writer.Write((int) RecordType.EndRecord);
            _writer.Write(0);
        }

        public void WriteValueLabelRecords(List<ShortValueLabels> records)
        {
            foreach (var nonStringValueLabels in records)
                WriteValueLabelRecord(nonStringValueLabels);
        }

        private void WriteValueLabelRecord(ShortValueLabels shortValueLabels)
        {
            _writer.Write((int) RecordType.ValueLabelRecord);
            _writer.Write(shortValueLabels.Labels.Count);
            foreach (var (value, label) in shortValueLabels.Labels)
            {
                if (value is string str)
                    _writer.Write(_encoding.GetPaddedValueAsByteArray(str, 8));
                else
                    _writer.Write(Convert.ToDouble(value));
                var (labelLength, labelBytes) = _encoding.GetValueLabelAsByteArray(label);
                _writer.Write(labelLength);
                _writer.Write(labelBytes);
            }

            // Writes the variableIndex where this label is used
            _writer.Write((int) RecordType.ValueLabelVariablesRecord);
            _writer.Write(shortValueLabels.VariableIndex.Count);
            foreach (var dictionaryIndex in shortValueLabels.VariableIndex) _writer.Write(dictionaryIndex);
        }

        private void WriteVariableRecord(VariableWrapper variable)
        {
            var valueLength = variable.ValueLength;
            var isString = variable.FormatType == FormatType.A;
            var length = !isString ? 0 : valueLength < 256 ? valueLength : 255;
            var missingValues = variable.MissingValuesObject;
            WriteVariable((uint) length, variable.Label, variable.MissingValueType, missingValues, variable.OutputFormat, variable.ShortName8Bytes);
            WriteBlankAndGhostRecords(variable);
        }

        private void WriteBlankAndGhostRecords(VariableWrapper variable)
        {
            var extraRecordCount = variable.FormatType != FormatType.A ? 0 : SpssMath.GetNumberOf32ByteBlocks(variable.ValueLength);
            for (var i = 1; i < extraRecordCount; i++)
            {
                if (i % 32 != 0)
                {
                    WriteBlankRecord();
                    continue;
                }

                var ghostIndex = i / 32;
                var len = ghostIndex == variable.GhostNames.Count ? variable.LastGhostVariableLength : 255;
                WriteGhostRecord(variable.GhostNames[ghostIndex - 1], len, variable.Label, variable.MissingValuesObject);
            }
        }

        private void WriteBlankRecord()
        {
            WriteVariable(uint.MaxValue, null, MissingValueType.NoMissingValues, Array.Empty<object>(), BlankOutputFormat, BlankShortName);
        }

        private void WriteGhostRecord(byte[] ghostName, int length, string? label, object[] missingValues)
        {
            var outputFormat = new OutputFormat(FormatType.A, length);
            WriteVariable((uint) length, label, MissingValueType.NoMissingValues, missingValues, outputFormat, ghostName);
        }

        private void WriteVariable(uint length, string? label, MissingValueType missingValueType, object[] missingValues, OutputFormat outputFormat, byte[] shortName)
        {
            _writer.Write((int) RecordType.VariableRecord);
            _writer.Write(length);
            _writer.Write(label != null ? 1 : 0);
            _writer.Write(length <= 8 ? (int) missingValueType : 0);
            _writer.Write(outputFormat.Value);
            _writer.Write(outputFormat.Value);
            _writer.Write(shortName);
            if (label != null)
                WriteVariableLabel(label);

            if (missingValueType != 0 && length <= 8)
                WriteShortMissing(missingValues);
        }

        private void WriteShortMissing(object[] missingValues)
        {
            foreach (var missingValue in missingValues)
                if (missingValue is string value)
                    _writer.Write(_encoding.GetPaddedValueAsByteArray(value, 8));
                else
                    _writer.Write((double) missingValue);
        }

        private void WriteVariableLabel(string label)
        {
            var (byteLength, labelBytes) = _encoding.GetVariableLabelAsByteArray(label!);
            _writer.Write((int) byteLength);
            _writer.Write(labelBytes);
        }
    }
}