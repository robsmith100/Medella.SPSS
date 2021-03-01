using System.Collections.Generic;
using System.IO;

namespace Curiosity.SPSS.FileParser.Records
{
    public class DocumentRecord : IRecord
    {
        public int LineCount { get; private set; }
        public IList<string>? LineCollection { get; private set; }
        public RecordType RecordType => RecordType.DocumentRecord;

        public void FillRecord(BinaryReader reader)
        {
            LineCount = reader.ReadInt32();
            LineCollection = new List<string>();
            for (var i = 0; i < LineCount; i++) LineCollection.Add(new string(reader.ReadChars(80)));
        }

        public void RegisterMetadata(MetaData metaData)
        {
            metaData.DocumentRecord = this;
        }

        public void WriteRecord(BinaryWriter writer)
        {
            writer.Write((int) RecordType);
            writer.Write(LineCount);
            foreach (var line in LineCollection!) writer.Write(line); // TODO proper encoding
        }
    }
}