using System.IO;
using System.Text;

namespace Spss.DataReaders;

public sealed class CompressedDataReader : CompressedDataReaderBase
{
    public CompressedDataReader(Stream stream, Encoding encoding, int bias) : base(stream, encoding, bias)
    {
    }
}