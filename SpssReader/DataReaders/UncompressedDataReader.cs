using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Spss.DataReaders;

public class UncompressedDataReader : CompressedDataReaderBase, IDataReader
{
    private readonly bool _isEndianCorrect;

    public UncompressedDataReader(Stream stream, Encoding encoding, bool isEndianCorrect) : base(stream, encoding, 0)
    {
        _isEndianCorrect = isEndianCorrect;
    }

    public new void ReadNumber(out double? doubleValue, out int? intValue)
    {
        EnsureBuffer(8);
        var value = MemoryMarshal.Read<long>(Buffer.AsSpan().Slice(BufferIndex, 8));
        BufferIndex += 8;
        if (!_isEndianCorrect) value = BinaryPrimitives.ReverseEndianness(value);
        doubleValue = BitConverter.Int64BitsToDouble(value);
        if (doubleValue is double.MinValue) doubleValue = null;
        intValue = null;
    }

    public new int ReadString(Span<byte> strArray, int length)
    {
        var blocks = GetBlockCount(length);
        EnsureBuffer(8 * blocks);
        var pos = 0;
        for (var i = 0; i < blocks; i++)
        {
            var bytes = MemoryMarshal.Read<long>(Buffer.AsSpan().Slice(BufferIndex, 8));
            MemoryMarshal.Write(strArray, ref bytes);
            BufferIndex += 8;
            pos += (i + 1) % 32 == 0 ? 7 : 8;
        }

        return GetTrimmedStringLength(strArray, pos);
    }
}