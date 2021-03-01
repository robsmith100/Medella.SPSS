using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Curiosity.SPSS.FileParser;
using Curiosity.SPSS.SpssDataset;

namespace Curiosity.SPSS.DataReader
{
    /// <summary>
    ///     Writes a spss data file to a stream
    /// </summary>
    public class SpssWriter : IDisposable
    {
        private readonly SavFileWriter _output;

        // TODO use read only collection and make it public

        /// <summary>
        ///     Creates a spss writer
        /// </summary>
        /// <param name="output">The binary stream to write to</param>
        /// <param name="variables">The variable collection to use</param>
        /// <param name="options"></param>
        public SpssWriter(Stream output, ICollection<Variable> variables, SpssOptions? options = null)
            : this(new SavFileWriter(output), variables, options)
        {
        }

        private SpssWriter(SavFileWriter output, ICollection<Variable> variables, SpssOptions? options = null)
        {
            _output = output;
            Variables = variables.ToList();
            Options = options ?? Options;
            WriteFileHeader();
        }

        /// <summary>
        ///     The file options used.
        /// </summary>
        public SpssOptions Options { get; } = new SpssOptions();

        /// <summary>
        ///     The variable collection. Once the writing has started, it should not be modified.
        /// </summary>
        public ICollection<Variable> Variables { get; }

        /// <summary>
        ///     Disposes the write stream
        /// </summary>
        public void Dispose()
        {
            _output.EndFile();
            _output.Dispose();
        }

        /// <summary>
        ///     Creates a record array with the variable count as length
        /// </summary>
        /// <returns></returns>
        public object?[] CreateRecord() => new object[Variables.Count];

        /// <summary>
        ///     Creates a record array for this file by using a Record object that could belong to another file.
        ///     It would contain the data from the original, but it would be resized to fit the current data variables.
        ///     To be able to copy to a new file, you must be careful that the variables from both are in the same order
        /// </summary>
        /// <param name="record">The record to get the data from.</param>
        /// <returns>A copy of the data record, resized for the current dataSet</returns>
        /// <remarks>
        ///     This method clones the record's data array and the resizes it to fit the current dataSet. Variables should
        ///     be in the same order for both records. If you are adding records, you might have to shift data to make it fit.
        ///     If the currentDataSet has less variables, the last values left will be lost.
        /// </remarks>
        public object[] CreateRecord(Record record)
        {
            var data = (object[]) record.Data.Clone();
            Array.Resize(ref data, Variables.Count);
            return data;
        }

        /// <summary>
        ///     Writes the record into the stream.
        /// </summary>
        /// <param name="record"></param>
        public void WriteRecord(object?[] record)
        {
            _output.WriteRecord(record);
        }

        private void WriteFileHeader()
        {
            _output.WriteFileHeader(Options, Variables);
        }

        /// <summary>
        ///     Finishes writing the file. If not used, last compressed values could be not written to the stream
        /// </summary>
        public void EndFile()
        {
            _output.EndFile();
        }
    }
}