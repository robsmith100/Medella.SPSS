using System.Collections.Generic;

namespace Spss.Models
{
    public class LongStringMissing
    {
        public LongStringMissing(byte[] variableName, List<byte[]> missingValues)
        {
            VariableName = variableName;
            MissingValues = missingValues;
        }

        public byte[] VariableName { get; set; }

        public List<byte[]> MissingValues { get; set; }
    }
}