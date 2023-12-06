using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spss.Models.ZLib
{
    internal class ZLibTrailer
    {
        // zlib data trailer (24 bytes)
        // as described here: https://www.gnu.org/software/pspp/pspp-dev/html_node/Data-Record.html

        /// <summary>
        /// The compression bias as a negative integer, e.g. if bias in the file header record is 100.0, then int_bias is −100(this is the only value yet observed in practice).
        /// </summary>
        public long int_bias;

        /// <summary>
        /// Always observed to be zero.
        /// </summary>
        public long zero;

        /// <summary>
        /// The number of bytes in each ZLIB compressed data block, except possibly the last, following decompression. Only 0x3ff000 has been observed so far.
        /// </summary>
        public int block_size;

        /// <summary>
        /// The number of ZLIB compressed data blocks, always exactly(ztrailer_len -24) / 24.
        /// </summary>
        public int n_blocks;

        /// <summary>
        /// describes the compressed data block corresponding to its offset
        /// </summary>
        public ZLibTrailerBlockDescriptor[]? block_descriptors;

    }
}
