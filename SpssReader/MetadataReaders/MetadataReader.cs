using System;
using Spss.FileStructure;
using Spss.MetadataReaders.Convertors;
using Spss.MetadataReaders.RecordReaders;
using Spss.Models;
using Spss.SpssMetadata;

namespace Spss.MetadataReaders;

public class MetadataReader
{
    private readonly MetadataInfo _metadataInfo;
    private readonly MetaDataStreamReader _metaDataStreamReader;
    private readonly RecordTypeInfoReader _recordTypeInfoReader;
    private readonly RecordTypeReader _recordTypeReader;

    public MetadataReader(MetaDataStreamReader metaDataStreamReader, MetadataInfo metadata)
    {
        _metaDataStreamReader = metaDataStreamReader;
        _metadataInfo = metadata;
        _recordTypeReader = new RecordTypeReader(metaDataStreamReader, _metadataInfo);
        _recordTypeInfoReader = new RecordTypeInfoReader(metaDataStreamReader, _metadataInfo);
    }

    public Metadata Read()
    {
        while (true)
        {
            var recordType = _metaDataStreamReader.ReadInt32();
            if (recordType == 0x02000000)
            {
                recordType = 2;
                _metaDataStreamReader.IsEndianCorrect = false;
            }

            GetRecordTypeReader(recordType)();
            if (recordType == (int)RecordType.EndRecord) break;
        }

        new MetadataConvertor(_metadataInfo, _metaDataStreamReader.IsEndianCorrect).Convert();
        return _metadataInfo.Metadata;
    }

    private Action GetRecordTypeReader(int recordType)
    {
        return recordType switch
        {
            (int)RecordType.HeaderRecord2 => _recordTypeReader.ReadHeaderRecord,
            (int)RecordType.HeaderRecord3 => _recordTypeReader.ReadHeaderRecord,
            (int)RecordType.VariableRecord => _recordTypeReader.ReadVariableRecord,
            (int)RecordType.ValueLabelRecord => _recordTypeReader.ReadValueLabelRecord,
            (int)RecordType.InfoRecord => _recordTypeInfoReader.ReadInfoRecord(),
            (int)RecordType.EndRecord => _recordTypeReader.ReadEndRecord,
            _ => throw new InvalidOperationException($"Unknown recordType {recordType:x8}")
        };
    }
}