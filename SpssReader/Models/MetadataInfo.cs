using System.Collections.Generic;
using Spss.FileStructure;
using Spss.SpssMetadata;

namespace Spss.Models;

public class MetadataInfo
{
    public Metadata Metadata { get; set; } = null!;
    public List<VariableProperties> Variables { get; set; } = new();
    public List<ShortValueLabel> ShortValueLabels { get; set; } = new();
    public List<LongValueLabel> LongValueLabels { get; set; } = new();
    public List<LongStringMissing> LongStringMissing { get; set; } = new();
    public List<DisplayParameter> DisplayParameters { get; set; } = new();
    public byte[]? LongVariableNames { get; set; }
    public byte[]? ValueLengthVeryLongString { get; set; }
}