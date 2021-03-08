using System;
using System.IO;
using Spss.Models;

namespace Spss
{
    public class Reader:BinaryReader
    {
        private readonly MetadataInfo _metadataInfo;

        public Reader(Stream stream, MetadataInfo metadataInfo) : base(stream)
        {
            _metadataInfo = metadataInfo;
        }

        public override int ReadInt32()
        {
            if (_metadataInfo.IsLittleEndian) return base.ReadInt32();
            var data = base.ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToInt32(data, 0);
        }
    }
}