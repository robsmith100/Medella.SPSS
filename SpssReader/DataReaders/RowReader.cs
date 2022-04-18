using System.Collections.Generic;
using System.Linq;
using Spss.SpssMetadata;

namespace Spss.DataReaders;

public class RowReader
{
    private readonly Reader _reader;
    public readonly List<Column> Columns;

    public RowReader(List<Variable> variables, Reader reader)
    {
        _reader = reader;
        Columns = variables.Select(variable => new Column(variable, _reader)).ToList();
    }

    public bool ReadRow()
    {
        if (_reader.IsEof())
            return false;
        foreach (var column in Columns)
        {
            column.ReadValue();
        }
        return true;
    }
}