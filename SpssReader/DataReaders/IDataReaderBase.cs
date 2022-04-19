using System;
using System.Text;

namespace Spss.DataReaders;

public interface IDataReader
{
    Encoding DataEncoding { get; }
    bool IsEof();
    void ReadNumber(out double? doubleValue, out int? intValue);
    int ReadString(Span<byte> strArray, int length);
}