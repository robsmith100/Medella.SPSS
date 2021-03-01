using System.Collections.Generic;
using SpssCommon.VariableModel;

namespace SpssCommon
{
    public class SpssData
    {
        public Metadata Metadata { get; set; } = null!;
        public List<object?> Data { get; set; } = new();
    }
}
