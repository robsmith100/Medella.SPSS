using System.Collections.Generic;
using Spss.FileStructure;
using Spss.SpssMetadata;

namespace Spss.Models
{
    public class MetadataInfo
    {
        public Metadata Metadata = null!;
        public List<VariableProperties> Variables { get; set; } = new List<VariableProperties>();
        public List<ShortValueLabel> ShortValueLabels { get; set; } = new List<ShortValueLabel>();
        public List<LongValueLabel> LongValueLabels { get; set; } = new List<LongValueLabel>();
        public List<LongStringMissing> LongStringMissing { get; set; } = new List<LongStringMissing>();
        public List<DisplayParameter> DisplayParameters { get; set; } = new List<DisplayParameter>();
        public byte[]? LongVariableNames { get; set; }
        public byte[]? ValueLengthVeryLongString { get; set; }
        public int Compressed { get; set; }
        public bool IsLittleEndian { get; set; } = true;
    }
}