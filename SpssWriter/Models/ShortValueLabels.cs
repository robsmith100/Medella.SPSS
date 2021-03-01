using System.Collections.Generic;

namespace Spss.Models
{
    public class ShortValueLabels
    {
        public IDictionary<object, string> Labels { get; init; } = null!;

        public List<int> VariableIndex { get; init; } = null!;
    }
}
