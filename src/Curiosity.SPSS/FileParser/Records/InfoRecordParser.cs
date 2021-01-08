using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace Curiosity.SPSS.FileParser.Records
{
    internal class InfoRecordParser : IRecordParser
    {
        private readonly IDictionary<int, Type> _infoRecordsTypes;

        public InfoRecordParser(IDictionary<int, Type> infoRecordsTypes)
        {
            _infoRecordsTypes = infoRecordsTypes;
        }

        public RecordType Accepts => RecordType.InfoRecord;

        public IRecord ParseRecord(BinaryReader reader)
        {
            var record = CreateRecord(reader);
            record.FillRecord(reader);
            return record;
        }

        private IRecord CreateRecord(BinaryReader reader)
        {
            var subType = reader.ReadInt32();
            var record = _infoRecordsTypes.TryGetValue(subType, out var type)
                ? (BaseInfoRecord) FormatterServices.GetUninitializedObject(type)
                : new UnknownInfoRecord(subType);

            // Check that we created the correct one
            if (record.SubType != subType)
                // if it gets to here, we fucked up registering the infoRecordsTypes when calling the constructor
                throw new Exception(
                    $"Wrong info record created for {subType}, obtained record instance for {record.SubType}. Please, fix the InfoRecordParser");

            return record;
        }
    }
}