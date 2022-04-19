using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Spss.FileStructure;

namespace Spss.DataReaders;

public abstract class CompressedDataReaderBase : IDataReader
{
    private bool _endOfStream;

    protected CompressedDataReaderBase(Stream stream, Encoding encoding, int bias)
    {
        Stream = stream;
        DataEncoding = encoding;
        Bias = bias;
        BufferLength = Buffer.Length;
        BufferIndex = BufferLength;
        _endOfStream = stream.Position == stream.Length;
    }

    protected byte[] Buffer { get; } = new byte[1024 * 1024 * 10];
    protected byte[] CompressedBlock { get; } = new byte[8];
    protected Stream Stream { get; }
    public int Bias { get; }
    protected int BufferIndex { get; set; }
    protected int BufferLength { get; set; }
    protected int CompressedBlockIndex { get; set; } = 8;

    public Encoding DataEncoding { get; init; }

    public bool IsEof()
    {
        if (!_endOfStream) return false;

        if (BufferIndex != BufferLength) return false;

        if (CompressedBlockIndex == 8) return true;

        return CompressedBlock[CompressedBlockIndex] == CompressedCode.Padding || CompressedBlock[CompressedBlockIndex] == CompressedCode.EndOfFile;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public virtual void ReadNumber(out double? doubleValue, out int? intValue)
    {
        if (CompressedBlockIndex == 8)
        {
            EnsureBuffer(8);
            var bytes = MemoryMarshal.Read<long>(Buffer.AsSpan().Slice(BufferIndex, 8));
            MemoryMarshal.Write(CompressedBlock, ref bytes);
            BufferIndex += 8;
            CompressedBlockIndex = 0;
        }

        var code = CompressedBlock[CompressedBlockIndex++];
        switch (code)
        {
            case CompressedCode.Uncompressed:
            {
                EnsureBuffer(8);
                var value = MemoryMarshal.Read<long>(Buffer.AsSpan().Slice(BufferIndex, 8));
                doubleValue = BitConverter.Int64BitsToDouble(value);
                intValue = null;
                BufferIndex += 8;
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
                throw new InvalidOperationException($"Unexpected compressed code:{Convert.ToHexString(CompressedBlock)}");
            default:
                intValue = code - Bias;
                doubleValue = null;
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public int ReadString(Span<byte> strArray, int length)
    {
        var blocks = GetBlockCount(length); // max size 1 compressed block for every 8 blocks + 1 extra
        var maxBlocks = (blocks + 7) / 8 + blocks + 1;
        EnsureBuffer(maxBlocks * 8);
        var pos = 0;
        for (var i = 0; i < blocks; i++)
        {
            if (!ReadString(strArray[pos..])) continue;

            pos += (i + 1) % 32 == 0 ? 7 : 8;
        }

        return GetTrimmedStringLength(strArray, pos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ReadString(Span<byte> destination)
    {
        if (CompressedBlockIndex == 8)
        {
            var bytes = MemoryMarshal.Read<long>(Buffer.AsSpan().Slice(BufferIndex, 8));
            MemoryMarshal.Write(CompressedBlock, ref bytes);
            CompressedBlockIndex = 0;
            BufferIndex += 8;
        }

        var code = CompressedBlock[CompressedBlockIndex++];
        switch (code)
        {
            case CompressedCode.SpaceCharsBlock:
                //skip spaces most of the time this is padding but could be a string with 8 space in the middle
                //ulong spaceBytes = 0x2020202020202020;
                //MemoryMarshal.Write(destination, ref spaceBytes);
                return false;
            case CompressedCode.Uncompressed:
            {
                var bytes = MemoryMarshal.Read<long>(Buffer.AsSpan().Slice(BufferIndex, 8));
                MemoryMarshal.Write(destination, ref bytes);
                BufferIndex += 8;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static int GetBlockCount(int length)
    {
        length = length / 252 * 256 + length % 252;
        var blocks = (length + 7) / 8;
        return blocks;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static int GetTrimmedStringLength(Span<byte> strArray, int pos)
    {
        while (pos > 0 && strArray[pos - 1] == ' ') pos--;

        return pos;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void EnsureBuffer(int length)
    {
        if (BufferIndex + length <= BufferLength || _endOfStream) return;

        FillBuffer();
    }

    private void FillBuffer()
    {
        var left = BufferLength - BufferIndex;
        if (left > 0) Buffer.AsSpan()[BufferIndex..].CopyTo(Buffer);

        BufferLength = left + Stream.Read(Buffer.AsSpan()[left..]);
        _endOfStream = BufferLength < Buffer.Length;
        BufferIndex = 0;
    }
}