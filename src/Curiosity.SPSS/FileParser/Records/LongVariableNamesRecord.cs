using System.Collections.Generic;
using System.Text;

namespace Curiosity.SPSS.FileParser.Records
{
    public class LongVariableNamesRecord : VariableDataInfoRecord<string>
    {
        public LongVariableNamesRecord(IDictionary<string, string?> variableLongNames, Encoding encoding)
            : base(variableLongNames, encoding)
        {
        }

        public override int SubType => InfoRecordType.LongVariableNames;

        public override void RegisterMetadata(MetaData metaData)
        {
            metaData.LongVariableNames = this;
            Metadata = metaData;
        }

        protected override string DecodeValue(string stringValue) => stringValue;

        protected override string EncodeValue(string value) => value;
    }
}