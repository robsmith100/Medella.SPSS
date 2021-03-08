using System;
using System.IO;
using Spss.FileStructure;
using Spss.MetadataReaders.Convertors;
using Spss.MetadataReaders.RecordReaders;
using Spss.Models;
using Spss.SpssMetadata;

namespace Spss.MetadataReaders
{
    public class MetadataReader
    {
        private readonly MetadataInfo _metadataInfo = new MetadataInfo();
        private readonly BinaryReader _reader;
        private readonly RecordTypeInfoReader _recordTypeInfoReader;
        private readonly RecordTypeReader _recordTypeReader;

        public MetadataReader(BinaryReader reader, Metadata metadata)
        {
            _reader = reader;
            _metadataInfo.Metadata = metadata;
            _recordTypeReader = new RecordTypeReader(reader, _metadataInfo);
            _recordTypeInfoReader = new RecordTypeInfoReader(reader, _metadataInfo);
        }

        public Metadata Read()
        {
            while (true)
            {
                var recordType = _reader.ReadInt32();
                GetRecordTypeReader(recordType)();
                if (recordType == (int) RecordType.EndRecord) break;
            }

            new MetadataConvertor(_metadataInfo).Convert();
            return _metadataInfo.Metadata;
        }

        private Action GetRecordTypeReader(int recordType)
        {
            return recordType switch
            {
                (int) RecordType.HeaderRecord => _recordTypeReader.ReadHeaderRecord,
                (int) RecordType.VariableRecord => _recordTypeReader.ReadVariableRecord,
                (int) RecordType.ValueLabelRecord => _recordTypeReader.ReadValueLabelRecord,
                // (int) RecordType.ValueLabelVariablesRecord => HeaderRecordReader,
                (int) RecordType.InfoRecord => _recordTypeInfoReader.ReadInfoRecord(),
                (int) RecordType.EndRecord => _recordTypeReader.ReadEndRecord,
                _ => throw new InvalidOperationException($"Unknown recordType {recordType:x8} at pos {_reader.BaseStream.Position - 4:x8}")
            };
        }
    }
}