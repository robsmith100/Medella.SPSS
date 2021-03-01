using System.Collections.Generic;
using System.Linq;
using Spss.Models;
using SpssCommon;
using SpssCommon.Models;

namespace Spss.VariableWriters.Generators
{
    public class DisplayValueGenerator
    {
        public static List<DisplayParameter> GenerateDisplayValues(List<VariableWrapper> variables)
        {
            List<DisplayParameter> displayValues = new();
            foreach (var variable in variables)
            {
                var displayValue = new DisplayParameter { Measure = variable.Measure, Columns = variable.Columns, Alignment = variable.Alignment };
                var namedVariables = SpssMath.GetNumberOfGhostVariables(variable.ValueLength) + 1;
                displayValues.AddRange(Enumerable.Range(1, namedVariables).Select(x => displayValue));
            }

            return displayValues;
        }
    }
}