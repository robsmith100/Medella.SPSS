using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spss.DataReaders;
using Spss.MetadataReaders;
using Spss.Models;
using Spss.SpssMetadata;

namespace Spss;

public class SpssReader
{
    private readonly MetadataInfo _metaDataInfo = new() { Metadata = new Metadata() };
    private readonly MetadataReader _metadataReader;
    private Metadata? _metadata;
    private RowReader? _rowReader;
    private readonly MetaDataStreamReader _metaDataStreamReader;


    public SpssReader(Stream fileStream)
    {
        _metaDataStreamReader = new MetaDataStreamReader(fileStream);
        _metadataReader = new MetadataReader(_metaDataStreamReader, _metaDataInfo);
    }

    public Metadata Metadata => _metadata ??= _metadataReader.Read();
    public RowReader RowReader => _rowReader ??= GetRowReader();


    public static SpssData Read(Stream stream)
    {
        var reader = new SpssReader(stream);
        var metadata = reader.Metadata;
        var data = new List<object?>();
        while (reader.RowReader.ReadRow()) data.AddRange(reader.RowReader.Columns.Select(column => column.GetValue()));

        return new SpssData { Metadata = metadata, Data = data };
    }

    private RowReader GetRowReader()
    {
        _metadata ??= _metadataReader.Read();
        return _rowReader ??= new RowReader(_metadata.Variables, _metaDataStreamReader.CreateDataReader());
    }
}