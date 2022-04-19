using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Spss.FileStructure;

namespace Spss.DataReaders;

public sealed class EndianCompressedDataReader : CompressedDataReaderBase
{
    public EndianCompressedDataReader(Stream stream, Encoding encoding, int bias) : base(stream, encoding, bias)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override void ReadNumber(out double? doubleValue, out int? intValue)
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
                doubleValue = BitConverter.Int64BitsToDouble(BinaryPrimitives.ReverseEndianness(value));
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
}