using System;
using System.Collections.Generic;
using System.IO;
using Spss.DataReaders;
using Spss.MetadataReaders;
using Spss.SpssMetadata;

namespace Spss
{
    public class SpssReader : IDisposable
    {
        private readonly DataReader _dataReader;
        private readonly Metadata _metaData = new Metadata(new List<Variable>());
        private readonly BinaryReader _reader;
        private readonly MetadataReader _metadataReader;


        private SpssReader(Stream fileStream)
        {
            _reader = new BinaryReader(fileStream);
            _metadataReader = new MetadataReader(_reader, _metaData);
            _dataReader = new DataReader(_reader, _metaData);
        }

        public void Dispose()
        {
            _reader.Dispose();
        }

        public static SpssData Read(Stream stream)
        {
            var reader = new SpssReader(stream);
            var metadata = reader._metadataReader.Read();
            var data = reader._dataReader.Read();
            reader.Dispose();
            return new SpssData { Metadata = metadata, Data = data };
        }
    }
}