using System.Collections.Generic;
using System.Linq;
using Spss.SpssMetadata;

namespace Spss.DataReaders;

public class RowReader
{
    private readonly IDataReader _dataReader;
    public readonly List<Column> Columns;

    public RowReader(List<Variable> variables, IDataReader dataReader)
    {
        _dataReader = dataReader;
        Columns = variables.Select(variable => new Column(variable, _dataReader)).ToList();
    }

    public bool ReadRow()
    {
        if (_dataReader.IsEof())
            return false;
        foreach (var column in Columns)
            column.ReadValue();
        return true;
    }
}