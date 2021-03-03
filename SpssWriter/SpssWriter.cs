using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Spss.DataWriters;
using Spss.MetadataWriters;
using SpssCommon;
using Spss.SpssMetadata;

namespace Spss
{
    public class SpssWriter : IDisposable
    {
        private readonly BinaryWriter _writer;
        public readonly DataWriter DataWriter;
        public readonly MetadataWriter MetadataWriter;


        public SpssWriter(List<Variable> variables, Stream stream) : this(new Metadata(variables), stream)
        {
        }

        public SpssWriter(Metadata metadata, Stream stream) : this(new SpssData { Data = new List<object?>(), Metadata = metadata }, stream)
        {
        }

        public SpssWriter(SpssData spssData, Stream stream)
        {
            _writer = new BinaryWriter(stream, Encoding.ASCII, true);
            MetadataWriter = new MetadataWriter(_writer, spssData.Metadata);
            DataWriter = new DataWriter(_writer, spssData);
        }

        void IDisposable.Dispose()
        {
            DataWriter.Flush();
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
            writer.MetadataWriter.Write();
            writer.DataWriter.Write();
            ((IDisposable) writer).Dispose();
        }
    }
}