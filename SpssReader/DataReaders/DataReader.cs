using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using SpssCommon.FileStructure;
using SpssCommon.SpssMetadata;

namespace Spss.DataReaders
{
    internal class DataReader
    {
        private readonly List<object?> _data = new();
        private readonly Metadata _metadata;
        private readonly BinaryReader _reader;
        private readonly byte[] _spacesBytes = Array.Empty<byte>();
        private readonly long _streamLength;
        private readonly byte[] _sysMiss = BitConverter.GetBytes(double.MinValue);
        private readonly byte[]?[] _uncompressedBuffer = new byte[8][];
        private Encoding _encoding = null!;
        private int _uncompressedBufferMax;
        private int _uncompressedBufferPosition;

        public DataReader(BinaryReader reader, Metadata metadata)
        {
            _reader = reader;
            _metadata = metadata;
            _streamLength = _reader.BaseStream.Length;
        }

        public List<object?> Read()
        {
            _encoding = Encoding.GetEncoding(_metadata.DataCodePage);
            while (true)
            {
                if (_reader.BaseStream.Position == _streamLength && _uncompressedBufferPosition == _uncompressedBufferMax)
                    return _data;
                foreach (var variable in _metadata.Variables)
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
            var get8Bytes = GetBlock();
            var value = BitConverter.ToDouble(get8Bytes);
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return value == double.MinValue ? null : value;
        }

        private string? ReadString(int length)
        {
            length = length / 252 * 256 + length % 252;
            var result = Enumerable.Range(0, (length + 7) / 8).Select(x => GetBlock()).Where(x => x.Length == 8).ToArray();
            var combine = Combine(result);
            var str = _encoding.GetString(combine).TrimEnd();
            return str == string.Empty ? null : str;
        }

        private static byte[] Combine(byte[][] arrays)
        {
            var len = arrays.Length * 8 / 256 * 255 + arrays.Length * 8 % 256;
            byte[] rv = new byte[len];
            var offset = 0;
            for (var i = 0; i < arrays.Length; i++)
            {
                byte[] array = arrays[i];
                Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += (i + 1) % 32 == 0 ? 7 : 8;
            }

            return rv;
        }

        private byte[] GetBlock()
        {
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
                if (code == CompressedCode.Padding) ;
                else if (code == CompressedCode.Uncompressed) todo.Add(position++);
                else if (code == CompressedCode.SpaceCharsBlock) _uncompressedBuffer[position++] = _spacesBytes;
                else if (code == CompressedCode.SysMiss) _uncompressedBuffer[position++] = _sysMiss;
                else if (code == CompressedCode.EndOfFile) break;
                else _uncompressedBuffer[position++] = BitConverter.GetBytes((double) (code - _metadata.Bias));

            foreach (var i in todo)
                _uncompressedBuffer[i] = _reader.ReadBytes(8);
            _uncompressedBufferMax = position;
        }
    }
}