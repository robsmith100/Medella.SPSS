using System.IO;
using System.Runtime.Serialization;

namespace Curiosity.SPSS.FileParser.Records
{
    internal interface IRecordParser
    {
        RecordType Accepts { get; }
        IRecord ParseRecord(BinaryReader reader);
    }

    internal class GeneralRecordParser<TRecord> : IRecordParser where TRecord : IRecord
    {
        public GeneralRecordParser(RecordType accepts)
        {
            Accepts = accepts;
        }

        public RecordType Accepts { get; }

        public IRecord ParseRecord(BinaryReader reader)
        {
            var record = CreateRecord();
            record.FillRecord(reader);
            return record;
        }

        private static TRecord CreateRecord() => (TRecord) FormatterServices.GetUninitializedObject(typeof(TRecord));
    }
}