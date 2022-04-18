using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Spss.Encodings;
using Spss.FileStructure;
using Spss.Models;
using Spss.SpssMetadata;

namespace Spss.MetadataReaders.RecordReaders;

public class RecordTypeInfoReader
{
    private readonly MetadataInfo _metadataInfo;
    private readonly Reader _reader;

    public RecordTypeInfoReader(Reader reader, MetadataInfo metadataInfo)
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
            _ => SkipInfoRecord
        };
    }

    private void ReadLongStringMissing()
    {
        _reader.ReadInt32(); //count=1
        var totLength = _reader.ReadInt32(); //length
        do
        {
            var varLength = _reader.ReadInt32();
            var variable = _reader.ReadBytes(varLength);
            var missingCount = _reader.ReadByte();
            var missingLength = _reader.ReadInt32();
            var missingValues = Enumerable.Range(0, missingCount).Select(_ => _reader.ReadBytes(missingLength).ToArray()).ToList();
            _metadataInfo.LongStringMissing.Add(new LongStringMissing(variable.ToArray(), missingValues));
            totLength -= 4 + varLength + 1 + 4 + missingCount * missingLength;
        } while (totLength > 0);
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
            return (value.ToArray(), label.ToArray());
        }).ToList();
        _metadataInfo.LongValueLabels.Add(new LongValueLabel(variable.ToArray(), labels));
    }

    private void ReadValueLengthVeryLongString()
    {
        _reader.ReadInt32();
        _metadataInfo.ValueLengthVeryLongString = _reader.ReadBytes(_reader.ReadInt32()).ToArray();
    }

    private void ReadCharacterEncoding()
    {
        _reader.ReadInt32();
        var bytes = _reader.ReadBytes(_reader.ReadInt32());
        var name = Encoding.ASCII.GetString(bytes);

        _metadataInfo.Metadata.DataCodePage =
            Encoding.GetEncodings().SingleOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.CodePage ??
            (int.TryParse(Regex.Match(name, @"\d+").Value, out var codePage)
                ? Encoding.GetEncodings().SingleOrDefault(x => x.CodePage == codePage)?.CodePage ?? Encoding.UTF8.CodePage
                : Encoding.UTF8.CodePage
            );
    }

    private void ReadLongVariableNames()
    {
        _reader.ReadInt32();
        _metadataInfo.LongVariableNames = _reader.ReadBytes(_reader.ReadInt32()).ToArray();
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
                Measure = (MeasurementType)_reader.ReadInt32(),
                Columns = items == 3 ? _reader.ReadInt32() : 5,
                Alignment = (Alignment)_reader.ReadInt32()
            });
            var ghosts = SpssMath.GetNumberOf256ByteBlocks(variable.ValueLength) - 1;
            _reader.Seek(ghosts * items * 4);
        }
    }

    private void SkipInfoRecord()
    {
        var size = _reader.ReadInt32();
        var count = _reader.ReadInt32();
        _reader.Seek(size * count); //skip info record
    }

    private void ReadMachineIntegerRecord()
    {
        _reader.Seek(4 + 4 + 6 * 4); //skip size+count+6 fields
        _reader.FileIsLittleEndian = _reader.ReadInt32() == 2; // 1 == BigEndian
        _metadataInfo.Metadata.DataCodePage = _metadataInfo.Metadata.HeaderCodePage = _reader.ReadInt32().GetCodePage();
    }
}