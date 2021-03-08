using System;
using System.Collections.Generic;
using System.IO;
using Spss.DataReaders;
using Spss.MetadataReaders;
using Spss.Models;
using Spss.SpssMetadata;

namespace Spss
{
    public class SpssReader : IDisposable
    {
        private readonly DataReader _dataReader;
        private readonly MetadataInfo _metaData = new MetadataInfo { Metadata = new Metadata(new List<Variable>()) };
        private readonly MetadataReader _metadataReader;
        private readonly Reader _reader;


        private SpssReader(Stream fileStream)
        {
            _reader = new Reader(fileStream, _metaData);
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
            return new SpssData { Metadata = metadata.Metadata, Data = data };
        }
    }
}