using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Spss.Encodings;
using Spss.Models;
using SpssCommon.FileStructure;
using SpssCommon.SpssMetadata;

namespace Spss.MetadataReaders.RecordReaders
{
    public class RecordTypeInfoReader
    {
        private readonly MetadataInfo _metadataInfo;
        private readonly BinaryReader _reader;

        public RecordTypeInfoReader(BinaryReader reader, MetadataInfo metadataInfo)
        {
            _reader = reader;
            _metadataInfo = metadataInfo;
        }

        public Action ReadInfoRecord()
        {
            var infoRecordType = _reader.ReadInt32();
            return infoRecordType switch
            {
                InfoRecordType.MachineInteger => ReadMachineIntegerRecord,
                InfoRecordType.MachineFloatingPoint => SkipInfoRecord,
                InfoRecordType.VariableDisplayParameter => ReadVariableDisplayParameters,
                InfoRecordType.LongVariableNames => ReadLongVariableNames,
                InfoRecordType.ValueLengthVeryLongString => ReadValueLengthVeryLongString,
                InfoRecordType.VariableAttributes => SkipInfoRecord,
                InfoRecordType.CharacterEncoding => ReadCharacterEncoding,
                InfoRecordType.ExtendedNumberOfCases => SkipInfoRecord,
                InfoRecordType.LongStringValueLabels => ReadLongStringValueLabels,
                InfoRecordType.LongStringMissing => ReadLongStringMissing,
                _ => SkipInfoRecord,
            };
        }

        private void ReadLongStringMissing()
        {
            _reader.ReadInt32(); //count=1
            _reader.ReadInt32(); //length
            var variable = _reader.ReadBytes(_reader.ReadInt32());
            var missingCount = _reader.ReadByte();
            var missingLength = _reader.ReadInt32();
            var missingValues = Enumerable.Range(0, missingCount).Select(_ => _reader.ReadBytes(missingLength)).ToList();
            _metadataInfo.LongStringMissing.Add(new LongStringMissing(variable, missingValues));
        }

        private void ReadLongStringValueLabels()
        {
            _reader.ReadInt32(); //count=1
            _reader.ReadInt32(); //length
            var variable = _reader.ReadBytes(_reader.ReadInt32());
            _reader.ReadInt32(); //length
            var labelCount = _reader.ReadInt32();
            var labels = Enumerable.Range(0, labelCount).Select(_ =>
            {
                var valueLength = _reader.ReadInt32();
                var value = _reader.ReadBytes(valueLength);
                var labelLength = _reader.ReadInt32();
                var label = _reader.ReadBytes(labelLength);
                return (value, label);
            }).ToList();
            _metadataInfo.LongValueLabels.Add(new LongValueLabel(variable, labels));
        }

        private void ReadValueLengthVeryLongString()
        {
            _reader.ReadInt32();
            _metadataInfo.ValueLengthVeryLongString = _reader.ReadBytes(_reader.ReadInt32());
        }

        private void ReadCharacterEncoding()
        {
            _reader.ReadInt32();
            var bytes = _reader.ReadBytes(_reader.ReadInt32());
            var name = Encoding.ASCII.GetString(bytes);

            _metadataInfo.Metadata.DataCodePage =
                Encoding.GetEncodings().SingleOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))?.CodePage ??
                (int.TryParse(Regex.Match(name, @"\d+").Value, out var codePage)
                    ? Encoding.GetEncodings().SingleOrDefault(x => x.CodePage == codePage)?.CodePage ?? Encoding.UTF8.CodePage
                    : Encoding.UTF8.CodePage
                );
        }

        private void ReadLongVariableNames()
        {
            _reader.ReadInt32();
            _metadataInfo.LongVariableNames = _reader.ReadBytes(_reader.ReadInt32());
        }

        private void ReadVariableDisplayParameters()
        {
            _reader.ReadInt32();
            var count = _reader.ReadInt32();
            var varCount = _metadataInfo.Variables.Select(x => SpssMath.GetNumberOf256ByteBlocks(x.ValueLength)).Aggregate((a, b) => a + b);
            var items = varCount * 3 == count ? 3 : 2;
            foreach (var variable in _metadataInfo.Variables)
            {
                _metadataInfo.DisplayParameters.Add(new DisplayParameter
                {
                    Measure = (MeasurementType) _reader.ReadInt32(),
                    Columns = items == 3 ? _reader.ReadInt32() : 5,
                    Alignment = (Alignment) _reader.ReadInt32(),
                });
                var ghosts = SpssMath.GetNumberOf256ByteBlocks(variable.ValueLength) - 1;
                _reader.BaseStream.Seek(ghosts * items * 4, SeekOrigin.Current);
            }
        }

        private void SkipInfoRecord()
        {
            var size = _reader.ReadInt32();
            var count = _reader.ReadInt32();
            _reader.BaseStream.Seek(size * count, SeekOrigin.Current); //skip info record
        }

        private void ReadMachineIntegerRecord()
        {
            _reader.BaseStream.Seek(4 + 4 + 7 * 4, SeekOrigin.Current); //skip size+count+7 fields
            _metadataInfo.Metadata.DataCodePage = _metadataInfo.Metadata.HeaderCodePage = _reader.ReadInt32().GetCodePage();
        }
    }
}