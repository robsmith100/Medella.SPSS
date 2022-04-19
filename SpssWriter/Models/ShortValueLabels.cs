using System.Collections.Generic;

namespace Spss.Models;

public class ShortValueLabels
{
    public IDictionary<object, string> Labels { get; set; } = null!;

    public List<int> VariableIndex { get; set; } = null!;
}