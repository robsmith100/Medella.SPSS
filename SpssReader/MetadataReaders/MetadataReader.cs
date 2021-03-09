using System;
using Spss.FileStructure;
using Spss.MetadataReaders.Convertors;
using Spss.MetadataReaders.RecordReaders;
using Spss.Models;

namespace Spss.MetadataReaders
{
    public class MetadataReader
    {
        private readonly MetadataInfo _metadataInfo;
        private readonly Reader _reader;
        private readonly RecordTypeInfoReader _recordTypeInfoReader;
        private readonly RecordTypeReader _recordTypeReader;

        public MetadataReader(Reader reader, MetadataInfo metadata)
        {
            _reader = reader;
            _metadataInfo = metadata;
            _recordTypeReader = new RecordTypeReader(reader, _metadataInfo);
            _recordTypeInfoReader = new RecordTypeInfoReader(reader, _metadataInfo);
        }

        public MetadataInfo Read()
        {
            while (true)
            {
                var recordType = _reader.ReadInt32();
                if (recordType == 0x02000000)
                {
                    recordType = 2;
                    _metadataInfo.IsLittleEndian = false;
                }

                GetRecordTypeReader(recordType)();
                if (recordType == (int) RecordType.EndRecord) break;
            }

            new MetadataConvertor(_metadataInfo).Convert();
            return _metadataInfo;
        }

        private Action GetRecordTypeReader(int recordType)
        {
            return recordType switch
            {
                (int) RecordType.HeaderRecord => _recordTypeReader.ReadHeaderRecord,
                (int) RecordType.VariableRecord => _recordTypeReader.ReadVariableRecord,
                (int) RecordType.ValueLabelRecord => _recordTypeReader.ReadValueLabelRecord,
                (int) RecordType.InfoRecord => _recordTypeInfoReader.ReadInfoRecord(),
                (int) RecordType.EndRecord => _recordTypeReader.ReadEndRecord,
                _ => throw new InvalidOperationException($"Unknown recordType {recordType:x8} at pos {_reader.BaseStream.Position - 4:x8}")
            };
        }
    }
}