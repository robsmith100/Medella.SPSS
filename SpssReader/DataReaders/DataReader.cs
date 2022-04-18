using System.Collections.Generic;
using Spss.SpssMetadata;

namespace Spss.DataReaders;

public class DataReader
{
    private readonly Reader _reader;
    private readonly List<Variable> _variables;

    public DataReader(Reader reader, List<Variable> variables)
    {
        _reader = reader;
        _variables = variables;
    }


    public RowReader CreateRowReader()
    {
        return new RowReader(_variables, _reader);
    }
}