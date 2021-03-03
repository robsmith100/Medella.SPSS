using System.Collections.Generic;
using System.Linq;
using Spss.Models;
using Spss.FileStructure;
using Spss.SpssMetadata;

namespace Spss.MetadataWriters.Generators
{
    public class ValueLabelIndexGenerator
    {
        public static List<ShortValueLabels> GenerateLabelIndexes(IEnumerable<VariableWrapper> variables)
        {
            var result = new List<ShortValueLabels>();
            var dictionaryIndex = 1;
            foreach (var variable in variables)
            {
                var isString = variable.FormatType == FormatType.A;
                if (variable.ValueLabels != null && variable.ValueLabels.Any() && (!isString || variable.ValueLength <= 8))
                {
                    var valueLabel = new ShortValueLabels
                    {
                        Labels = variable.ValueLabels!.ToDictionary(p => p.Key, p => p.Value),
                        VariableIndex = new List<int> { dictionaryIndex }
                    };
                    result.Add(valueLabel);
                }

                dictionaryIndex += isString ? SpssMath.GetNumberOf32ByteBlocks(variable.ValueLength) : 1;
            }

            return result;
        }
    }
}