using System.Collections.Generic;
using System.Linq;
using Spss.FileStructure;
using Spss.Models;

namespace Spss.MetadataWriters.Generators;

public class DisplayValueGenerator
{
    public static List<DisplayParameter> GenerateDisplayValues(List<VariableWrapper> variables)
    {
        var displayValues = new List<DisplayParameter>();
        foreach (var variable in variables)
        {
            var displayValue = new DisplayParameter { Measure = variable.Measure, Columns = variable.Columns, Alignment = variable.Alignment };
            var namedVariables = SpssMath.GetNumberOfGhostVariables(variable.ValueLength) + 1;
            displayValues.AddRange(Enumerable.Range(1, namedVariables).Select(_ => displayValue));
        }

        return displayValues;
    }
}