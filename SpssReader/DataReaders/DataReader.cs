using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Spss.FileStructure;
using Spss.Models;
using Spss.SpssMetadata;

namespace Spss.DataReaders
{
    internal class DataReader
    {
        private readonly List<object?> _data = new List<object?>();
        private readonly MetadataInfo _metadataInfo;
        private readonly BinaryReader _reader;
        private readonly byte[] _spacesBytes = Array.Empty<byte>();
        private readonly long _streamLength;
        private readonly byte[] _sysMiss = BitConverter.GetBytes(double.MinValue);
        private readonly byte[]?[] _uncompressedBuffer = new byte[8][];
        private Encoding _encoding = null!;
        private int _uncompressedBufferMax;
        private int _uncompressedBufferPosition;

        public DataReader(BinaryReader reader, MetadataInfo metadataInfo)
        {
            _reader = reader;
            _metadataInfo = metadataInfo;
            _streamLength = _reader.BaseStream.Length;
        }

        public List<object?> Read()
        {
            _encoding = Encoding.GetEncoding(_metadataInfo.Metadata.DataCodePage);
            while (true)
            {
                if (_reader.BaseStream.Position == _streamLength && _uncompressedBufferPosition == _uncompressedBufferMax)
                    return _data;
                foreach (var variable in _metadataInfo.Metadata.Variables)
                {
                    var formatType = variable.FormatType;
                    if (formatType == FormatType.A) _data.Add(ReadString(variable.SpssWidth));
                    else if (formatType.IsDate()) _data.Add(ReadDouble()?.AsDate());
                    else _data.Add(ReadDouble());
                }
            }
        }

        private double? ReadDouble()
        {
            var bytes = GetBlock();
            var value = _metadataInfo.ConvertDouble(bytes);
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return value == double.MinValue ? (double?)null : value;
        }


        private string? ReadString(int length)
        {
            length = length / 252 * 256 + length % 252;
            var result = Enumerable.Range(0, (length + 7) / 8).Select(_ => GetBlock()).Where(x => x.Length == 8).ToArray();
            var combine = Combine(result);
            var str = _encoding.GetString(combine).TrimEnd();
            return str == string.Empty ? null : str;
        }

        private static byte[] Combine(byte[][] arrays)
        {
            var arraysLength = arrays.Length;
            var len = arraysLength * 8 / 256 * 255 + arraysLength * 8 % 256;
            if (arraysLength > 0 && arraysLength % 32 == 0)
                len++; // the last block is always 8 bytes and is padded with a space
            var rv = new byte[len];
            var offset = 0;
            for (var i = 0; i < arraysLength; i++)
            {
                byte[] array = arrays[i];
                Buffer.BlockCopy(array, 0, rv, offset, array.Length); //todo: use span and long to speedup copy
                offset += (i + 1) % 32 == 0 ? 7 : 8;
            }

            return rv;
        }

        private byte[] GetBlock()
        {
            if (_metadataInfo.Compressed == 0)
                return _reader.ReadBytes(8);
            if (_uncompressedBufferPosition < _uncompressedBufferMax) return _uncompressedBuffer[_uncompressedBufferPosition++] ?? Array.Empty<byte>();
            DecompressData();
            return _uncompressedBuffer[_uncompressedBufferPosition++] ?? Array.Empty<byte>();
        }


        private void DecompressData()
        {
            _uncompressedBufferPosition = 0;
            var position = 0;
            var todo = new List<int>();
            var readBytes = _reader.ReadBytes(8);
            Debug.Assert(readBytes.Length == 8, "End of stream?");
            foreach (var code in readBytes)
                if (code == CompressedCode.Padding) { }
                else if (code == CompressedCode.Uncompressed) todo.Add(position++);
                else if (code == CompressedCode.SpaceCharsBlock) _uncompressedBuffer[position++] = _spacesBytes;
                else if (code == CompressedCode.SysMiss) _uncompressedBuffer[position++] = _sysMiss;
                else if (code == CompressedCode.EndOfFile) break;
                else _uncompressedBuffer[position++] = _metadataInfo.ConvertDouble(code - _metadataInfo.Metadata.Bias);

            foreach (var i in todo)
                _uncompressedBuffer[i] = _reader.ReadBytes(8);
            _uncompressedBufferMax = position;
        }
    }
}