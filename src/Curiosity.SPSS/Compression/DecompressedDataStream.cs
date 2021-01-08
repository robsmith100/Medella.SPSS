using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Curiosity.SPSS.Compression
{
    internal class DecompressedDataStream : Stream
    {
        private const int InstructionSetByteSize = 8;
        private const string SpaceString = "        ";
        private readonly byte[][] _elementBuffer = new byte[8][];

        private readonly long _position = 0;

        private readonly BinaryReader _reader;
        private readonly byte[] _spacesBytes;

        private readonly byte[] _systemMissingBytes;
        private int _elementBufferPosition;
        private int _elementBufferSize;
        private int _inElementPosition; // for those rare cases where we end up in the middle of an element.

        public DecompressedDataStream(Stream compressedDataStream, double bias, double systemMissing)
        {
            CompressedDataStream = compressedDataStream;
            Bias = bias;
            SystemMissing = systemMissing;
            _reader = new BinaryReader(compressedDataStream, Encoding.ASCII);

            _spacesBytes = Encoding.ASCII.GetBytes(SpaceString);
            _systemMissingBytes = BitConverter.GetBytes(SystemMissing);
        }

        public Stream CompressedDataStream { get; }
        public double Bias { get; }
        public double SystemMissing { get; }

        public override bool CanRead => CompressedDataStream.CanRead;

        public override bool CanSeek => CompressedDataStream.CanSeek;

        public override bool CanWrite => CompressedDataStream.CanWrite;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            CompressedDataStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // Usually we can just send out the next 8-byte element.
            if (count == 8 && offset == 0 && _inElementPosition == 0)
            {
                if (!PreserveBuffer()) return 0;
                _elementBuffer[_elementBufferPosition++].CopyTo(buffer, offset);
                return 8;

                // End of stream:
            }

            // Else we have to run thru the bytes one by one:

            for (var i = 0; i < count; i++)
            {
                // Check for the unlikely case that the byte-request runs over multiple elements
                if (_inElementPosition == 8)
                {
                    // Flow over to next 8-byte element
                    _elementBufferPosition++;
                    _inElementPosition = 0;
                }

                if (PreserveBuffer())
                    buffer[i + offset] = _elementBuffer[_elementBufferPosition][_inElementPosition];
                else
                    // End of stream:
                    return 0;
            }

            return count;
        }

        private bool PreserveBuffer()
        {
            // Check whether the end of internal buffer is reached 
            if (_elementBufferPosition < _elementBufferSize) return true;
            if (!ParseNextInstructionSet()) return false; // End of stream
            _elementBufferPosition = 0;
            return true;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        private bool ParseNextInstructionSet()
        {
            var instructionSet = _reader.ReadBytes(InstructionSetByteSize);

            if (instructionSet.Length < InstructionSetByteSize)
                // End of stream.
                return false;


            var uncompressedElementBufferPositions = new List<int>();
            var bufferPosition = 0;

            for (var i = 0; i < InstructionSetByteSize; i++)
            {
                int instruction = instructionSet[i];

                switch (instruction)
                {
                    //padding
                    case 0:
                        break;
                    // compressed value
                    case > 0 and < 252:
                    {
                        // compute actual value:
                        var value = instruction - Bias;
                        _elementBuffer[bufferPosition++] = BitConverter.GetBytes(value);
                        break;
                    }
                    // end of file
                    case 252:
                        _elementBufferSize = bufferPosition;
                        return false;
                    // uncompressed value
                    case 253:
                        uncompressedElementBufferPositions.Add(bufferPosition++);
                        break;
                    // space string
                    case 254:
                        _elementBuffer[bufferPosition++] = _spacesBytes;
                        break;
                    // system missing value
                    case 255:
                        _elementBuffer[bufferPosition++] = _systemMissingBytes;
                        break;
                }
            }

            _elementBufferSize = bufferPosition;

            // Read the uncompressed values (they follow after the instruction set):
            foreach (var pos in uncompressedElementBufferPositions) _elementBuffer[pos] = _reader.ReadBytes(8);
            return true;
        }
    }
}