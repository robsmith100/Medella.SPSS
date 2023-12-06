using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spss.Models.ZLib
{
    internal class ZLibTrailerBlockDescriptor
    {
        // zlib block descriptor
        // as described here: https://www.gnu.org/software/pspp/pspp-dev/html_node/Data-Record.html

        /// <summary>
        /// The offset, in bytes, that this block of data would have in a similar system file that uses compression format
        /// 1. This is zheader_ofs in the first block descriptor, and in each succeeding block descriptor it is the sum of
        /// the previous desciptor’s uncompressed_ofs and uncompressed_size.
        /// </summary>
        public long uncompressed_ofs;

        /// <summary>
        /// The offset, in bytes, of the actual beginning of this compressed data block. This is zheader_ofs + 24 in the
        /// first block descriptor, and in each succeeding block descriptor it is the sum of the previous descriptor’s
        /// compressed_ofs and compressed_size. The final block descriptor’s compressed_ofs and compressed_size sum to
        /// ztrailer_ofs.
        /// </summary>
        public long compressed_ofs;

        /// <summary>
        /// The number of bytes in this data block, after decompression. This is block_size in every data block except
        /// the last, which may be smaller.
        /// </summary>
        public int uncompressed_size;

        /// <summary>
        /// The number of bytes in this data block, as stored compressed in this system file.
        /// </summary>
        public int compressed_size;
    }
}
