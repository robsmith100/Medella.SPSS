using System;
using System.Collections.Generic;
using System.Linq;
using Spss.DataReaders;
using Spss.FileStructure;
using Spss.Models;
using Spss.SpssMetadata;

namespace Spss.MetadataReaders.RecordReaders;

public class RecordTypeReader
{
    private readonly MetadataInfo _metadataInfo;

    private readonly MetaDataStreamReader _metaDataStreamReader;
    private int _currentIndex = 1;

    public RecordTypeReader(MetaDataStreamReader metaDataStreamReader, MetadataInfo metadataInfo)
    {
        _metaDataStreamReader = metaDataStreamReader;
        _metadataInfo = metadataInfo;
    }

    public void ReadEndRecord()
    {
        _metaDataStreamReader.Seek(4);
    }

    public void ReadHeaderRecord()
    {
        _metaDataStreamReader.Seek(60);
        var headerCode = _metaDataStreamReader.ReadInt32();
        _metaDataStreamReader.IsEndianCorrect = headerCode != 0x02000000;
        _metaDataStreamReader.Seek(4);
        _metaDataStreamReader.DataStreamContentType = (DataStreamContentType)_metaDataStreamReader.ReadInt32();
        _metaDataStreamReader.Seek(4);
        _metadataInfo.Metadata.Cases = _metaDataStreamReader.ReadInt32();
        _metadataInfo.Metadata.Bias = (int)_metaDataStreamReader.ReadDouble();
        _metaDataStreamReader.Seek(17 + 64 + 3);
        _metaDataStreamReader.Bias = _metadataInfo.Metadata.Bias;
    }

    public void ReadValueLabelRecord()
    {
        var count = _metaDataStreamReader.ReadInt32();
        var labels = new List<(byte[] Value, byte[] Label)>();
        for (var i = 0; i < count; i++)
        {
            var value = _metaDataStreamReader.ReadBytes(8);
            var length = _metaDataStreamReader.ReadByte();
            var label = _metaDataStreamReader.ReadBytes(length);
            _metaDataStreamReader.ReadBytes((length + 1 + 7) / 8 * 8 - (length + 1));
            labels.Add((value.ToArray(), label.ToArray()));
        }

        _metaDataStreamReader.ReadInt32();
        count = _metaDataStreamReader.ReadInt32();
        var indexes = Enumerable.Range(0, count).Select(_ => _metaDataStreamReader.ReadInt32()).ToList();
        _metadataInfo.ShortValueLabels.Add(new ShortValueLabel(labels, indexes));
    }

    public void ReadVariableRecord()
    {
        var valueLength = _metaDataStreamReader.ReadInt32();
        var hasVariableLabel = _metaDataStreamReader.ReadInt32() == 1;
        var missingValueType = _metaDataStreamReader.ReadInt32();
        _metaDataStreamReader.ReadInt32(); //print format
        var decimalPlaces = _metaDataStreamReader.ReadByte();
        var spssWidth = _metaDataStreamReader.ReadByte();
        var formatType = _metaDataStreamReader.ReadByte();
        _metaDataStreamReader.ReadByte();
        var shortName = _metaDataStreamReader.ReadBytes(8);

        var properties = new VariableProperties { FormatType = (FormatType)formatType, Index = _currentIndex, MissingValueType = missingValueType, ShortName = shortName.ToArray(), DecimalPlaces = decimalPlaces };
        if (hasVariableLabel) properties.Label = ReadLabel();

        if (Math.Abs(missingValueType) != 0) ReadMissing(properties);

        var blockWidth = ReadBlankRecords(valueLength);

        properties.ValueLength = blockWidth;
        properties.SpssWidth = formatType == (int)FormatType.A ? blockWidth : spssWidth;
        _metadataInfo.Variables.Add(properties);
        _currentIndex += SpssMath.GetNumberOf32ByteBlocks(blockWidth);
    }

    private void ReadMissing(VariableProperties properties)
    {
        properties.Missing = Enumerable.Range(0, Math.Abs(properties.MissingValueType)).Select(_ => _metaDataStreamReader.ReadBytes(8).ToArray()).ToArray();
    }

    private byte[] ReadLabel()
    {
        var length = _metaDataStreamReader.ReadInt32();
        var label = _metaDataStreamReader.ReadBytes(length);
        _metaDataStreamReader.ReadBytes((length + 3) / 4 * 4 - length);
        return label.ToArray();
    }

    private int ReadBlankRecords(int valueLength)
    {
        var lengthTotal = 0;
        if (valueLength <= 8) return 8;

        var skip = SpssMath.GetAllocatedSize(valueLength);
        lengthTotal += skip == 256 ? 252 : valueLength;
        skip -= 8;
        _metaDataStreamReader.Seek(skip * 4);
        return lengthTotal;
    }
}