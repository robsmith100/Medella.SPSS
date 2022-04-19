using System.Collections.Generic;

namespace Spss.Models;

public class LongValueLabel
{
    public LongValueLabel(byte[] variableName, List<(byte[] Value, byte[] Label)> valueLabels)
    {
        VariableName = variableName;
        ValueLabels = valueLabels;
    }

    public byte[] VariableName { get; set; }

    public List<(byte[] Value, byte[] Label)> ValueLabels { get; set; }
}