using System.Collections.Generic;

namespace Spss.Models;

public class ShortValueLabel
{
    public ShortValueLabel(List<(byte[] value, byte[] label)> labels, List<int> indexes)
    {
        Labels = labels;
        Indexes = indexes;
    }

    public List<(byte[] value, byte[] label)> Labels { get; set; }

    public List<int> Indexes { get; }
}