using Spss.Models.ZLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spss.DataReaders
{
    internal class ZLibStreamReader : Stream
    {
        // https://www.gnu.org/software/pspp/pspp-dev/html_node/Data-Record.html

        private static int _BLOCK_BUFFER_SIZE = 10 * 1024 * 1024; // 10 MB

        public ZLibStreamReader(Stream stream)
        {
            this.stream = stream;

            this.header = ReadHeader();

            // seek past the data blocks to the trailer to read blocks info
            this.trailer = SeekAndReadTrailer(this.header);

            // seek back to first data block, just after the header
            stream.Seek(this.header.zheader_ofs + 24, SeekOrigin.Begin);

        }

        private Stream stream;
        private ZLibHeader header;
        private ZLibTrailer trailer;

        // for reading block chunks:
        int _block_index = 0;
        byte[] _block_buffer = new byte[_BLOCK_BUFFER_SIZE];
        int _block_buffer_position = -1;
        int _block_buffer_len = 0;
        byte[] _CMF_FLG = new byte[2]; // for reading zip header
        DeflateStream? _block_stream;
        private byte[] minibuf = new byte[8]; // for reading ints

        private int stream_ReadInt32()
        {
            int nBytesRead = stream.Read(minibuf, 0, 4);
            if (nBytesRead != 4) throw new Exception("End of file reached");
            return BitConverter.ToInt32(minibuf, 0);
        }

        private long stream_ReadInt64()
        {
            int nBytesRead = stream.Read(minibuf, 0, 8);
            if (nBytesRead != 8) throw new Exception("End of file reached");
            return BitConverter.ToInt64(minibuf, 0);
        }

        private ZLibHeader ReadHeader()
        {
            var header = new ZLibHeader(); // (24 bytes)

            // The offset, in bytes, of the beginning of this structure within the system file.
            header.zheader_ofs = stream_ReadInt64();

            // The offset, in bytes, of the first byte of the ZLIB data trailer.
            header.ztrailer_ofs = stream_ReadInt64();

            // The number of bytes in the ZLIB data trailer. This and the previous field sum to the size of the system file in bytes.
            header.ztrailer_len = stream_ReadInt64();

            return header;
        }

        private ZLibTrailer SeekAndReadTrailer(ZLibHeader header)
        {
            // seeking is requires since the trailer is located after the data blocks
            stream.Seek(header.ztrailer_ofs, SeekOrigin.Begin);

            var trailer = new ZLibTrailer(); // (24 bytes)

            // The compression bias as a negative integer, e.g. if bias in the file header record is 100.0, then int_bias is −100(this is the only value yet observed in practice).
            trailer.int_bias = stream_ReadInt64();

            // int64 zero;
            // Always observed to be zero.
            trailer.zero = stream_ReadInt64();

            // int32 block_size;
            // The number of bytes in each ZLIB compressed data block, except possibly the last, following decompression. Only 0x3ff000 has been observed so far.
            trailer.block_size = stream_ReadInt32();

            // int32 n_blocks;
            // The number of ZLIB compressed data blocks, always exactly(ztrailer_len -24) / 24.
            trailer.n_blocks = stream_ReadInt32();

            trailer.block_descriptors = new ZLibTrailerBlockDescriptor[trailer.n_blocks];
            for (int i = 0; i < trailer.n_blocks; i++)
            {
                ZLibTrailerBlockDescriptor bd = new ZLibTrailerBlockDescriptor();
                trailer.block_descriptors[i] = bd;

                bd.uncompressed_ofs = stream_ReadInt64();
                bd.compressed_ofs = stream_ReadInt64();
                bd.uncompressed_size = stream_ReadInt32();
                bd.compressed_size = stream_ReadInt32();
            }

            return trailer;

        }

        private bool ReadBlockPiece()
        {
            if (_block_index >= trailer.n_blocks)
            {
                return false;
            }

            if (_block_buffer_position == -1) // first time, need to initialize stream
            {
                //CMF | FLG |
                //0x78 | 0x01 - No Compression / low
                //0x78 | 0x9C - Default Compression
                //0x78 | 0xDA - Best Compression
                stream.Read(_CMF_FLG, 0, 2); // discard the compression code

                _block_stream = new DeflateStream(stream, CompressionMode.Decompress, true);
                _block_buffer_position = 0;
            }

            int bytes_read = _block_stream?.Read(_block_buffer, 0, _BLOCK_BUFFER_SIZE) ?? 0;
            if (bytes_read > 0)
            {
                _block_buffer_position = 0;
                _block_buffer_len = bytes_read;
                return true;
            }
            else
            {
                // nothing read, let's initialize a new stream if possible
                _block_index++;
                if (_block_index >= trailer.n_blocks)
                    return false;

                // dispose the old block stream
                _block_stream?.Dispose();

                var _block = trailer.block_descriptors[_block_index];

                // do we need to seek just in case?
                stream.Seek(_block.compressed_ofs, SeekOrigin.Begin);

                //CMF | FLG |
                //0x78 | 0x01 - No Compression / low
                //0x78 | 0x9C - Default Compression
                //0x78 | 0xDA - Best Compression
                stream.Read(_CMF_FLG, 0, 2); // discard the compression code

                _block_stream = new DeflateStream(stream, CompressionMode.Decompress, true);

                // try to read a block of data
                bytes_read = _block_stream.Read(_block_buffer, 0, _BLOCK_BUFFER_SIZE);
                if (bytes_read > 0)
                {
                    _block_buffer_position = 0;
                    _block_buffer_len = bytes_read;
                    return true;
                }
                return false;
            }

        }


        public override bool CanRead => throw new NotImplementedException();

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => stream.Length;

        public override long Position
        {
            get => stream.Position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int n_bytes_read = 0;
            while (n_bytes_read < count)
            {

                if (_block_buffer_position >= _block_buffer_len || _block_buffer_position == -1)
                {
                    ReadBlockPiece();
                }

                if (_block_buffer_position >= _block_buffer_len || _block_buffer_position == -1)
                {
                    return n_bytes_read; // can't read any more, so return
                }

                buffer[offset + n_bytes_read] = _block_buffer[_block_buffer_position];

                n_bytes_read++;
                _block_buffer_position++;
            }
            return n_bytes_read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
