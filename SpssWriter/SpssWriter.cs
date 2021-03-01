using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Spss.DataWriters;
using Spss.VariableWriters;
using SpssCommon;
using SpssCommon.VariableModel;

namespace Spss
{
    public class SpssWriter : IDisposable
    {
        private readonly BinaryWriter _writer;
        public readonly DataWriter DataWriter;
        public readonly VariableWriter VariableWriter;


        public SpssWriter(List<Variable> variables, Stream stream) : this(new Metadata(variables), stream)
        {
        }

        public SpssWriter(Metadata metadata, Stream stream) : this(new SpssData { Data = new List<object?>(), Metadata = metadata }, stream)
        {
        }

        public SpssWriter(SpssData spssData, Stream stream)
        {
            _writer = new BinaryWriter(stream,Encoding.ASCII,true);
            VariableWriter = new VariableWriter(_writer, spssData.Metadata);
            DataWriter = new DataWriter(_writer, spssData);
        }

        void IDisposable.Dispose()
        {
            DataWriter.CloseFile();
            _writer.Flush();
        }

        public static void Write(List<Variable> variables, IEnumerable<IEnumerable<object?>> data, Stream stream)
        {
            Write(new SpssData { Metadata = new Metadata(variables), Data = data.SelectMany(x => x).ToList() }, stream);
        }

        public static void Write(List<Variable> variables, List<object?> data, Stream stream)
        {
            Write(new SpssData { Metadata = new Metadata(variables), Data = data }, stream);
        }

        public static void Write(SpssData spssData, Stream stream)
        {
            var writer = new SpssWriter(spssData, stream);
            writer.VariableWriter.Write();
            writer.DataWriter.Write();
            ((IDisposable) writer).Dispose();
        }
    }
}
