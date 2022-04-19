using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Spss.DataReaders;

namespace Spss.MetadataReaders;

public class MetaDataStreamReader
{
    private readonly byte[] _int = new byte[4];
    private readonly byte[] _int64 = new byte[8];
    private readonly Stream _stream;
    public bool IsEndianCorrect { get; set; } = true;
    public int Bias { get; set; } = 0;
    public DataStreamContentType DataStreamContentType { get; set; } = DataStreamContentType.UnCompressed;
    public Encoding DataEncoding { get; set; } = Encoding.ASCII;

    public MetaDataStreamReader(Stream stream)
    {
        _stream = stream;
    }

    public IDataReader CreateDataReader()
    {
        if (DataStreamContentType == DataStreamContentType.UnCompressed)
            return new UncompressedDataReader(_stream, DataEncoding, IsEndianCorrect);
        if (IsEndianCorrect)
            return new CompressedDataReader(_stream, DataEncoding, Bias);
        return new EndianCompressedDataReader(_stream, DataEncoding, Bias);
    }

    public int ReadInt32()
    {
        var _ = _stream.Read(_int, 0, 4);
        var value = MemoryMarshal.Read<int>(_int);
        if (!IsEndianCorrect) value = BinaryPrimitives.ReverseEndianness(value);
        return value;
    }

    public double ReadDouble()
    {
        var _ = _stream.Read(_int64, 0, 8);
        var value = MemoryMarshal.Read<long>(_int64);
        if (!IsEndianCorrect) value = BinaryPrimitives.ReverseEndianness(value);
        return BitConverter.Int64BitsToDouble(value);
    }

    public Span<byte> ReadBytes(int varLength)
    {
        var data = new byte[varLength];
        var _ = _stream.Read(data);
        return data;
    }

    public byte ReadByte()
    {
        return (byte)_stream.ReadByte();
    }

    public void Seek(int count)
    {
        _stream.Seek(count, SeekOrigin.Current);
    }
}