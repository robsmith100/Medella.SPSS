using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Curiosity.SPSS.FileParser.Records
{
    public class VeryLongStringRecord : VariableDataInfoRecord<int>
    {
        public VeryLongStringRecord(IDictionary<string, int> dictionary, Encoding encoding)
            : base(dictionary, encoding)
        {
        }

        protected override bool UsesTerminator => true;

        public override int SubType => InfoRecordType.VeryLongString;

        protected override int DecodeValue(string stringValue)
        {
            if (!int.TryParse(stringValue, out var length))
                throw new SpssFileFormatException("Couldn't read the size of the VeryLongString as integer. Value read was '" +
                                                  (stringValue.Length > 80 ? stringValue.Substring(0, 77) + "..." : stringValue) + "'");

            return length;
        }

        protected override string EncodeValue(int value)
        {
            var strValue = value.ToString(CultureInfo.InvariantCulture);
            return strValue + '\0';
        }

        public override void RegisterMetadata(MetaData metaData)
        {
            metaData.VeryLongStrings = this;
            Metadata = metaData;
        }
    }
}