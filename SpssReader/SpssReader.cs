using System;
using System.Collections.Generic;
using System.IO;
using Spss.DataReaders;
using Spss.Models;
using Spss.VariableReaders;
using SpssCommon;
using SpssCommon.VariableModel;

namespace Spss
{
    public class SpssReader : IDisposable
    {
        private readonly DataReader _dataReader;
        private readonly Metadata _metaData = new(new List<Variable>());
        private readonly BinaryReader _reader;
        private readonly VariableReader _variableReader;


        private SpssReader(Stream fileStream)
        {
            _reader = new BinaryReader(fileStream);
            _variableReader = new VariableReader(_reader, _metaData);
            _dataReader = new DataReader(_reader, _metaData);
        }

        public void Dispose()
        {
            _reader.Dispose();
        }

        public static SpssData Read(Stream stream)
        {
            var reader = new SpssReader(stream);
            var metadata = reader._variableReader.Read();
            var data = reader._dataReader.Read();
            return new SpssData { Metadata = metadata, Data = data };
        }
    }
}
