using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Spss.DataReaders;
using Spss.FileStructure;

namespace Spss;

public class Reader
{
    private readonly byte[] _buffer = new byte[1024 * 1024 * 10];
    private readonly byte[] _compressedBlock = new byte[8];
    private readonly Stream _stream;
    private int _bufferIndex;
    private int _bufferLength;
    private int _compressedBlockIndex = 8;
    private bool _endOfStream;
    private bool _fileIsLittleEndian = true;
    public int Bias;
    public CompressedType CompressedType = CompressedType.UnCompressed;
    public Encoding DataEncoding = Encoding.ASCII;
    public bool IsEndianCorrect;

    public Reader(Stream stream)
    {
        _stream = stream;
        _bufferLength = _buffer.Length;
        _bufferIndex = _bufferLength;
        FileIsLittleEndian = true;
    }

    public bool FileIsLittleEndian
    {
        get => _fileIsLittleEndian;
        set
        {
            _fileIsLittleEndian = value;
            IsEndianCorrect = _fileIsLittleEndian == BitConverter.IsLittleEndian;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureBuffer(int length)
    {
        if (_bufferIndex + length <= _bufferLength || _endOfStream) return;
        FillBuffer();
    }

    private void FillBuffer()
    {
        var left = _bufferLength - _bufferIndex;
        _buffer.AsSpan()[_bufferIndex..].CopyTo(_buffer);

        _bufferLength = left + _stream.Read(_buffer.AsSpan()[left..]);
        _endOfStream = _bufferLength < _buffer.Length;
        _bufferIndex = 0;
    }

    public bool IsEof()
    {
        if (!_endOfStream) return false;
        if (_bufferIndex != _bufferLength) return false;
        if (_compressedBlockIndex == 8) return true;
        return _compressedBlock[_compressedBlockIndex] == CompressedCode.Padding || _compressedBlock[_compressedBlockIndex] == CompressedCode.EndOfFile;
    }

    public int ReadInt32()
    {
        EnsureBuffer(4);
        var value = MemoryMarshal.Read<int>(_buffer.AsSpan().Slice(_bufferIndex, 4));
        _bufferIndex += 4;
        return IsEndianCorrect ? value : BinaryPrimitives.ReverseEndianness(value);
    }

    public Span<byte> ReadBytes(int varLength)
    {
        EnsureBuffer(varLength);
        var span = _buffer.AsSpan().Slice(_bufferIndex, varLength);
        _bufferIndex += varLength;
        return span;
    }

    public byte ReadByte()
    {
        EnsureBuffer(1);
        var value = _buffer.AsSpan()[_bufferIndex];
        _bufferIndex += 1;
        return value;
    }

    public double ReadDouble()
    {
        EnsureBuffer(8);
        var value = MemoryMarshal.Read<long>(_buffer.AsSpan().Slice(_bufferIndex, 8));
        _bufferIndex += 8;
        return IsEndianCorrect ? BitConverter.Int64BitsToDouble(value) : BitConverter.Int64BitsToDouble(BinaryPrimitives.ReverseEndianness(value));
    }

    public int ReadString(Span<byte> strArray, int length)
    {
        var blocks = GetBlockCount(length);
        var pos = 0;
        for (var i = 0; i < blocks; i++)
        {
            EnsureBuffer(8);
            var bytes = MemoryMarshal.Read<long>(_buffer.AsSpan().Slice(_bufferIndex, 8));
            MemoryMarshal.Write(strArray, ref bytes);
            _bufferIndex += 8;
            pos += (i + 1) % 32 == 0 ? 7 : 8;
        }

        return GetTrimmedStringLength(strArray, pos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetBlockCount(int length)
    {
        length = length / 252 * 256 + length % 252;
        var blocks = (length + 7) / 8;
        return blocks;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetTrimmedStringLength(Span<byte> strArray, int pos)
    {
        while (pos > 0 && strArray[pos - 1] == ' ')
            pos--;
        return pos;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public int ReadStringCompressed(Span<byte> strArray, int length)
    {
        var blocks = GetBlockCount(length); // max size (blocks*9)*8 // 1 compressed block for every 8 blocks + 1 extra
        EnsureBuffer((blocks * 9 + 1) * 8);
        var pos = 0;
        for (var i = 0; i < blocks; i++)
        {
            if (!ReadStringCompressed(strArray[pos..])) continue;
            pos += (i + 1) % 32 == 0 ? 7 : 8;
        }

        return GetTrimmedStringLength(strArray, pos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ReadStringCompressed(Span<byte> destination)
    {
        if (_compressedBlockIndex == 8)
        {
            var bytes = MemoryMarshal.Read<long>(_buffer.AsSpan().Slice(_bufferIndex, 8));
            MemoryMarshal.Write(_compressedBlock, ref bytes);
            _compressedBlockIndex = 0;
            _bufferIndex += 8;
        }

        var code = _compressedBlock[_compressedBlockIndex++];
        switch (code)
        {
            case CompressedCode.SpaceCharsBlock:
                //skip spaces most of the time this is padding but could be a string with 8 space in the middle
                //ulong spaceBytes = 0x2020202020202020;
                //MemoryMarshal.Write(destination, ref spaceBytes);
                return false;
            case CompressedCode.Uncompressed:
                {
                    var bytes = MemoryMarshal.Read<long>(_buffer.AsSpan().Slice(_bufferIndex, 8));
                    MemoryMarshal.Write(destination, ref bytes);
                    _bufferIndex += 8;
                    return true;
                }
            default:
                // padding: is used at the end of a file and should not happen when reading a string
                // SysMiss: string doesn't have sysMiss this way
                // EndOfFile
                // Compressed double (code-bias): Is not valid when reading string
                return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void ReadNumberCompressed(ref double? doubleValue, ref int? intValue)
    {
        if (_compressedBlockIndex == 8)
        {
            EnsureBuffer(8);
            var bytes = MemoryMarshal.Read<long>(_buffer.AsSpan().Slice(_bufferIndex, 8));
            MemoryMarshal.Write(_compressedBlock, ref bytes);
            _bufferIndex += 8;
            _compressedBlockIndex = 0;
        }

        var code = _compressedBlock[_compressedBlockIndex++];
        switch (code)
        {
            case CompressedCode.Uncompressed:
                {
                    EnsureBuffer(8);
                    var value = MemoryMarshal.Read<long>(_buffer.AsSpan().Slice(_bufferIndex, 8));
                    if (!IsEndianCorrect) value = BinaryPrimitives.ReverseEndianness(value);
                    doubleValue = BitConverter.Int64BitsToDouble(value);
                    intValue = null;
                    _bufferIndex += 8;
                    return;
                }
            case CompressedCode.SysMiss:
                {
                    intValue = null;
                    doubleValue = null;
                    return;
                }
            case CompressedCode.SpaceCharsBlock:
            case CompressedCode.Padding:
            case CompressedCode.EndOfFile:
                throw new InvalidOperationException($"Unexpected compressed code:{Convert.ToHexString(_compressedBlock)}");
            default:
                intValue = code - Bias;
                doubleValue = null;
                break;
        }
    }

    public string GetErrorInfo(int offset)
    {
        return $"BufferIndex:{_bufferIndex + offset:x8}, data:{Convert.ToHexString(_buffer.AsSpan().Slice(_bufferIndex + offset, 128).ToArray())}";
    }

    public void Seek(int count)
    {
        EnsureBuffer(count);
        _bufferIndex += count; // No boundCheck implemented Seek is only header and assuming header is smaller than bufferSize
    }
}