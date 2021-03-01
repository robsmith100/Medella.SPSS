using System;
using System.Collections.Generic;
using System.Text;

namespace SpssCommon.VariableModel
{
    public class Metadata
    {
        /// <summary>
        ///     Creates Metadata with defaults.
        ///     bias=100 &amp; encodings=UTF8
        /// </summary>
        public Metadata(List<Variable> variables)
        {
            // Default values
            Bias = 100;
            Cases = -1;
            HeaderCodePage = Encoding.UTF8.CodePage;
            DataCodePage = Encoding.UTF8.CodePage;
            Variables = variables;
        }

        /// <summary>
        ///     A bias used for the compression of numerical values. By default set to 100.
        ///     <para />
        ///     Only integers between (1 - bias) and (251 - bias) will be compressed into only one byte.
        ///     If the number has decimals or is not in that range, it will be written as a 8-byte double
        /// </summary>
        public int Bias { get; set; }

        /// <summary>
        ///     Number of cases in file, or -1 if unknown.
        /// </summary>
        public int Cases { get; set; }

        /// <summary>
        ///     The encoding used to read/write the variable
        /// </summary>
        public int HeaderCodePage { get; set; }

        /// <summary>
        ///     The encoding used to read/write the cases
        /// </summary>
        public int DataCodePage { get; set; }

        /// <summary>
        ///     Variable used
        /// </summary>
        public List<Variable> Variables { get; set; }
    }
}
