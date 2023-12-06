using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spss.Models.ZLib
{
    internal class ZLibHeader
    {
        // zlib data header (24 bytes)
        // as described here: https://www.gnu.org/software/pspp/pspp-dev/html_node/Data-Record.html

        /// <summary>
        /// The offset, in bytes, of the beginning of this structure within the system file.
        /// </summary>
        public long zheader_ofs;

        /// <summary>
        /// The offset, in bytes, of the first byte of the ZLIB data trailer.
        /// </summary>
        public long ztrailer_ofs;

        /// <summary>
        /// The number of bytes in the ZLIB data trailer. This and the previous field sum to the size of the system file in bytes.
        /// </summary>
        public long ztrailer_len;

    }
}
