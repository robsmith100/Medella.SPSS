using System.Collections.Generic;

namespace Spss.Models
{
    public class LongValueLabel
    {
        public LongValueLabel(byte[] variableName, List<(byte[] value, byte[] label)> valueLabels)
        {
            VariableName = variableName;
            ValueLabels = valueLabels;
        }

        public byte[] VariableName { get; set; }

        public List<(byte[] value, byte[] label)> ValueLabels { get; set; }
    }
}