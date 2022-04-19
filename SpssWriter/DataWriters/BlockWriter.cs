using System;
using System.Diagnostics;
using System.IO;
using Spss.FileStructure;

namespace Spss.DataWriters;

public class BlockWriter
{
    private const int BlockSize = 8;
    private readonly byte[] _uncompressedBuffer = new byte[(BlockSize + 1) * BlockSize];
    private readonly BinaryWriter _writer;
    private int _blockIndex;
    private int _uncompressedIndex;

    public BlockWriter(BinaryWriter writer)
    {
        _writer = writer;
    }

    private int FullBlocksCount => _uncompressedIndex / BlockSize;

    public int AddPadding()
    {
        if (_uncompressedIndex % BlockSize == 0) return 0;
        var remaining = BlockSize - _uncompressedIndex % BlockSize;
        for (var i = 0; i < remaining; i++)
            _uncompressedBuffer[_uncompressedIndex++] = 0x20;
        WriteCompressedCode(CompressedCode.Uncompressed);
        return remaining;
    }

    public void AddSpace()
    {
        _uncompressedBuffer[_uncompressedIndex++] = 0x20;
        if (_uncompressedIndex % BlockSize == 0)
            WriteCompressedCode(CompressedCode.Uncompressed);
    }

    public void WriteToUncompressedBuffer(byte[] bytes)
    {
        Buffer.BlockCopy(bytes, 0, _uncompressedBuffer, _uncompressedIndex, BlockSize);
        _uncompressedIndex += BlockSize;
    }

    public void WriteCompressedCode(byte code)
    {
        // Increment the compressed codes counter for the block and write the code to the output
        _blockIndex++;
        _writer.Write(code);
        if (_blockIndex < BlockSize) return;
        _blockIndex = 0;
        WriteUncompressedBlocks();
    }

    public void WriteUncompressedBytes(byte[] bytes, int start, int length)
    {
        var currentUncompressedBlock = FullBlocksCount;

        Buffer.BlockCopy(bytes, start, _uncompressedBuffer, _uncompressedIndex, length);
        _uncompressedIndex += length;

        if (currentUncompressedBlock != FullBlocksCount)
            WriteCompressedCode(CompressedCode.Uncompressed);
    }

    private void WriteUncompressedBlocks()
    {
        if (_uncompressedIndex < 0) return;
        // Get the amount of uncompressed bytes  ready to be written 
        var currentFullBlockIndex = FullBlocksCount * BlockSize;
        WriteCompleteBlocks(currentFullBlockIndex);

        _uncompressedIndex -= currentFullBlockIndex;
        Debug.Assert(_uncompressedIndex == 0, "_uncompressedIndex should be 0");
    }

    public void Flush()
    {
        if (_blockIndex == 0) return;
        for (var i = _blockIndex; i < BlockSize; i++) WriteCompressedCode(CompressedCode.Padding);
        WriteUncompressedBlocks();
        _writer.Flush();
    }


    private void WriteCompleteBlocks(int currentFullBlockIndex)
    {
        _writer.Write(_uncompressedBuffer, 0, currentFullBlockIndex);
    }
}