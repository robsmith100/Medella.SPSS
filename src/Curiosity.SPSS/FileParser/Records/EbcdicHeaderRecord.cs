using System;
using System.IO;

namespace Curiosity.SPSS.FileParser.Records
{
    internal class EbcdicHeaderRecord : IRecord
    {
        public static NotSupportedException Exception => new("EBCDIC???? Who uses that? Honestly!!");

        public RecordType RecordType => RecordType.EbcdicHeaderRecord;

        public void WriteRecord(BinaryWriter writer)
        {
            throw Exception;
        }

        public void FillRecord(BinaryReader reader)
        {
            throw Exception;
        }

        public void RegisterMetadata(MetaData metaData)
        {
            throw Exception;
        }
    }
}