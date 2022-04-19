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
    private readonly MetaDataStreamReader _metaDataStreamReader;

    public RecordTypeInfoReader(MetaDataStreamReader metaDataStreamReader, MetadataInfo metadataInfo)
    {
        _metaDataStreamReader = metaDataStreamReader;
        _metadataInfo = metadataInfo;
    }

    public Action ReadInfoRecord()
    {
        var infoRecordType = _metaDataStreamReader.ReadInt32();
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
        _metaDataStreamReader.ReadInt32(); //count=1
        var totLength = _metaDataStreamReader.ReadInt32(); //length
        do
        {
            var varLength = _metaDataStreamReader.ReadInt32();
            var variable = _metaDataStreamReader.ReadBytes(varLength);
            var missingCount = _metaDataStreamReader.ReadByte();
            var missingLength = _metaDataStreamReader.ReadInt32();
            var missingValues = Enumerable.Range(0, missingCount).Select(_ => _metaDataStreamReader.ReadBytes(missingLength).ToArray()).ToList();
            _metadataInfo.LongStringMissing.Add(new LongStringMissing(variable.ToArray(), missingValues));
            totLength -= 4 + varLength + 1 + 4 + missingCount * missingLength;
        } while (totLength > 0);
    }

    private void ReadLongStringValueLabels()
    {
        _metaDataStreamReader.ReadInt32(); //count=1
        _metaDataStreamReader.ReadInt32(); //length
        var variable = _metaDataStreamReader.ReadBytes(_metaDataStreamReader.ReadInt32());
        _metaDataStreamReader.ReadInt32(); //length
        var labelCount = _metaDataStreamReader.ReadInt32();
        var labels = Enumerable.Range(0, labelCount).Select(_ =>
        {
            var valueLength = _metaDataStreamReader.ReadInt32();
            var value = _metaDataStreamReader.ReadBytes(valueLength);
            var labelLength = _metaDataStreamReader.ReadInt32();
            var label = _metaDataStreamReader.ReadBytes(labelLength);
            return (value.ToArray(), label.ToArray());
        }).ToList();
        _metadataInfo.LongValueLabels.Add(new LongValueLabel(variable.ToArray(), labels));
    }

    private void ReadValueLengthVeryLongString()
    {
        _metaDataStreamReader.ReadInt32();
        _metadataInfo.ValueLengthVeryLongString = _metaDataStreamReader.ReadBytes(_metaDataStreamReader.ReadInt32()).ToArray();
    }

    private void ReadCharacterEncoding()
    {
        _metaDataStreamReader.Seek(4);
        var length = _metaDataStreamReader.ReadInt32();
        var bytes = _metaDataStreamReader.ReadBytes(length);
        var name = Encoding.ASCII.GetString(bytes);
        _metadataInfo.Metadata.DataCodePage = GetCodePage(name);
        _metaDataStreamReader.DataEncoding = Encoding.GetEncoding(_metadataInfo.Metadata.DataCodePage);
    }

    private static int GetCodePage(string name)
    {
        return Encoding.GetEncodings().SingleOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.CodePage ??
               (int.TryParse(Regex.Match(name, @"\d+").Value, out var codePage)
                   ? Encoding.GetEncodings().SingleOrDefault(x => x.CodePage == codePage)?.CodePage ?? Encoding.UTF8.CodePage
                   : Encoding.UTF8.CodePage
               );
    }

    private void ReadLongVariableNames()
    {
        _metaDataStreamReader.ReadInt32();
        _metadataInfo.LongVariableNames = _metaDataStreamReader.ReadBytes(_metaDataStreamReader.ReadInt32()).ToArray();
    }

    private void ReadVariableDisplayParameters()
    {
        _metaDataStreamReader.ReadInt32();
        var count = _metaDataStreamReader.ReadInt32();
        var varCount = _metadataInfo.Variables.Select(x => SpssMath.GetNumberOf256ByteBlocks(x.ValueLength)).Aggregate((a, b) => a + b);
        var items = varCount * 3 == count ? 3 : 2;
        foreach (var variable in _metadataInfo.Variables)
        {
            _metadataInfo.DisplayParameters.Add(new DisplayParameter
            {
                Measure = (MeasurementType)_metaDataStreamReader.ReadInt32(),
                Columns = items == 3 ? _metaDataStreamReader.ReadInt32() : 5,
                Alignment = (Alignment)_metaDataStreamReader.ReadInt32()
            });
            var ghosts = SpssMath.GetNumberOf256ByteBlocks(variable.ValueLength) - 1;
            _metaDataStreamReader.Seek(ghosts * items * 4);
        }
    }

    private void SkipInfoRecord()
    {
        var size = _metaDataStreamReader.ReadInt32();
        var count = _metaDataStreamReader.ReadInt32();
        _metaDataStreamReader.Seek(size * count); //skip info record
    }

    private void ReadMachineIntegerRecord()
    {
        _metaDataStreamReader.Seek(4 + 4 + 7 * 4); //skip size+count+7 fields //skip (also skip field 7 Endian)
        _metadataInfo.Metadata.DataCodePage = _metadataInfo.Metadata.HeaderCodePage = _metaDataStreamReader.ReadInt32().GetCodePage();
        _metaDataStreamReader.DataEncoding = Encoding.GetEncoding(_metadataInfo.Metadata.DataCodePage);
    }
}