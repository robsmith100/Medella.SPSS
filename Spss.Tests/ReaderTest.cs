using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Spss;
using Spss.DataReaders;
using Xunit;
using static System.Environment;

// ReSharper disable IdentifierTypo

// ReSharper disable StringLiteralTypo

namespace SPSS.Tests;

public class ReaderTest
{
    private const string ReadTestSav = @"{'Metadata':
{'Bias':100,'Cases':3,'HeaderCodePage':1252,'DataCodePage':1252,'Variables':[
{'Name':'varenie','FormatType':5,'SpssWidth':8,'DecimalPlaces':2,'Label':'varaible \u00F1','ValueLabels':{'1':'ni\u00F1o','2':'ni\u00F1a'},'MissingValueType':0,'MissingValues':[],'Columns':8,'Alignment':1,'MeasurementType':3},
{'Name':'street','FormatType':1,'SpssWidth':300,'DecimalPlaces':0,'Label':'stra\u00DFe','ValueLabels':null,'MissingValueType':0,'MissingValues':[],'Columns':31,'Alignment':1,'MeasurementType':3},
{'Name':'cross','FormatType':5,'SpssWidth':8,'DecimalPlaces':2,'Label':'\u00FCber die Stra\u00DFe gehen','ValueLabels':null,'MissingValueType':0,'MissingValues':[],'Columns':8,'Alignment':1,'MeasurementType':3}]},
'Data':[1,'Landsberger Stra\u00DFe',1,2,'Fr\u00F6belplatz',5,1,'Bayerstra\u00DFe',500]}";

    private const string MissingValuesSav = @"{'Metadata':{'Bias':100,'Cases':8,'HeaderCodePage':65001,'DataCodePage':65001,'Variables':[
{'Name':'NoMissing','FormatType':5,'SpssWidth':8,'DecimalPlaces':2,'Label':null,'ValueLabels':null,'MissingValueType':0,'MissingValues':[],'Columns':8,'Alignment':1,'MeasurementType':1},
{'Name':'OneDiscrete','FormatType':5,'SpssWidth':8,'DecimalPlaces':2,'Label':null,'ValueLabels':null,'MissingValueType':1,'MissingValues':[1],'Columns':8,'Alignment':1,'MeasurementType':1},
{'Name':'TwoDiscrete','FormatType':5,'SpssWidth':8,'DecimalPlaces':2,'Label':null,'ValueLabels':null,'MissingValueType':2,'MissingValues':[1,2],'Columns':8,'Alignment':1,'MeasurementType':1},
{'Name':'ThreeDiscrete','FormatType':5,'SpssWidth':8,'DecimalPlaces':2,'Label':null,'ValueLabels':null,'MissingValueType':3,'MissingValues':[1,2,3],'Columns':8,'Alignment':1,'MeasurementType':1},
{'Name':'Range','FormatType':5,'SpssWidth':8,'DecimalPlaces':2,'Label':null,'ValueLabels':null,'MissingValueType':-2,'MissingValues':[1,5],'Columns':8,'Alignment':1,'MeasurementType':1},
{'Name':'RangeDiscrete','FormatType':5,'SpssWidth':8,'DecimalPlaces':2,'Label':null,'ValueLabels':null,'MissingValueType':-3,'MissingValues':[1,5,7],'Columns':8,'Alignment':1,'MeasurementType':1}]},
'Data':[0,0,0,0,0,0,1,1,1,1,1,1,2,2,2,2,2,2,3,3,3,3,3,3,4,4,4,4,4,4,5,5,5,5,5,5,6,6,6,6,6,6,7,7,7,7,7,7]}";

    private const string CakeSpss1000SimilarvarsSav = @"{'Metadata':{'Bias':100,'Cases':1,'HeaderCodePage':65001,'DataCodePage':65001,'Variables':[
{'Name':'cake','FormatType':1,'SpssWidth':1000,'DecimalPlaces':0,'Label':'test','ValueLabels':null,'MissingValueType':0,'MissingValues':[],'Columns':8,'Alignment':0,'MeasurementType':1},
{'Name':'ekacekac','FormatType':1,'SpssWidth':1000,'DecimalPlaces':0,'Label':'tset','ValueLabels':null,'MissingValueType':0,'MissingValues':[],'Columns':8,'Alignment':0,'MeasurementType':1},
{'Name':'ekacekab','FormatType':1,'SpssWidth':1000,'DecimalPlaces':0,'Label':'blaa','ValueLabels':null,'MissingValueType':0,'MissingValues':[],'Columns':8,'Alignment':0,'MeasurementType':1}]},'Data':[
null,null,'xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx']}";

    private const string LongstringSav = @"{'Metadata':{'Bias':100,'Cases':0,'HeaderCodePage':65001,'DataCodePage':65001,'Variables':[
{'Name':'a\uD83E\uDD13\uD83E\uDD13','FormatType':1,'SpssWidth':9,'DecimalPlaces':0,'Label':'label for name1','ValueLabels':{'\uD83E\uDD13\uD83E\uDD13b':'b\uD83E\uDD13\uD83E\uDD13','a':'Label for a','b':'Label for b'},'MissingValueType':2,'MissingValues':['a','b'],'Columns':8,'Alignment':0,'MeasurementType':1}]},'Data':[]}";

    private const string BigEndian = @"{'Metadata':{'Bias':100,'Cases':50,'HeaderCodePage':20127,'DataCodePage':20127,'Variables':[
{'Name':'ID','FormatType':5,'SpssWidth':5,'DecimalPlaces':0,'Label':null,'ValueLabels':null,'MissingValueType':0,'MissingValues':[],'Columns':5,'Alignment':1,'MeasurementType':1},
{'Name':'SEX','FormatType':5,'SpssWidth':5,'DecimalPlaces':0,'Label':null,'ValueLabels':{'1':'MALE','2':'FEMALE'},'MissingValueType':0,'MissingValues':[],'Columns':5,'Alignment':1,'MeasurementType':1},
{'Name':'GROUP','FormatType':5,'SpssWidth':5,'DecimalPlaces':0,'Label':null,'ValueLabels':{'0':'Control','1':'Treatment'},'MissingValueType':0,'MissingValues':[],'Columns':5,'Alignment':1,'MeasurementType':1},
{'Name':'AGE','FormatType':5,'SpssWidth':5,'DecimalPlaces':0,'Label':null,'ValueLabels':null,'MissingValueType':0,'MissingValues':[],'Columns':5,'Alignment':1,'MeasurementType':1},
{'Name':'PRETEST','FormatType':5,'SpssWidth':5,'DecimalPlaces':0,'Label':null,'ValueLabels':null,'MissingValueType':0,'MissingValues':[],'Columns':5,'Alignment':1,'MeasurementType':1},
{'Name':'POSTTEST','FormatType':5,'SpssWidth':5,'DecimalPlaces':0,'Label':null,'ValueLabels':null,'MissingValueType':0,'MissingValues':[],'Columns':5,'Alignment':1,'MeasurementType':1}]},'Data':[
1,1,0,16,36.37699261646427,53.321541454892696,2,2,0,5,55.067910808804164,64.9424958680917,3,2,1,13,52.73154603476168,61.62222317860627,4,2,0,14,58.5724579698679,73.24317759180526,5,1,0,6,41.049722164549244,54.15160962726405,6,2,0,12,76.09519377518654,111.01127943470202,7,1,1,6,43.38608693859173,50.83133693777862,8,2,0,27,50.39518126071919,62.8673254371633,9,2,0,7,66.7497346790166,67.43270038520576,10,1,0,13,69.08609945305909,60.79215500623491,11,2,1,6,59.74064035688914,59.1320186614922,12,2,0,7,56.23609319582541,65.77256404046305,13,2,0,28,41.049722164549244,62.452291350977625,14,2,1,4,43.38608693859173,59.96208683386355,15,1,0,8,71.42246422710157,56.641814144378124,16,1,1,26,43.38608693859173,47.511064248293195,17,1,1,9,52.73154603476168,66.60263221283441,18,2,1,6,64.41336990497412,61.62222317860627,19,2,0,6,66.7497346790166,65.77256404046305,20,1,0,5,41.049722164549244,55.81174597200677,21,1,0,12,45.72245171263422,51.661405110149985,22,1,1,5,57.40427558284665,57.47188231674948,23,2,0,18,52.73154603476168,62.452291350977625,24,2,1,10,55.067910808804164,62.452291350977625,25,2,1,16,52.73154603476168,57.47188231674948,26,2,1,7,57.40427558284665,66.60263221283441,27,1,1,17,43.38608693859173,49.17120059303591,28,1,1,7,45.72245171263422,50.83133693777862,29,1,0,7,43.38608693859173,53.321541454892696,30,1,0,4,45.72245171263422,60.79215500623491,31,2,1,6,64.41336990497412,60.79215500623491,32,1,1,11,48.058816486676704,53.321541454892696,33,2,1,17,62.077005130931624,57.47188231674948,34,2,1,9,48.058816486676704,59.96208683386355,35,2,1,13,57.40427558284665,58.30195048912084,36,1,0,19,36.37699261646427,55.81174597200677,37,1,1,20,50.39518126071919,49.17120059303591,38,1,0,6,69.08609945305909,58.30195048912084,39,1,1,6,41.049722164549244,59.96208683386355,40,1,1,15,45.72245171263422,55.81174597200677,41,1,0,20,48.058816486676704,58.30195048912084,42,1,1,7,57.40427558284665,61.62222317860627,43,1,0,17,43.38608693859173,73.24317759180526,44,1,1,7,45.72245171263422,54.15160962726405,45,1,0,7,43.38608693859173,111.01127943470202,46,2,0,4,45.72245171263422,50.83133693777862,47,2,1,6,64.41336990497412,62.8673254371633,48,2,0,11,48.058816486676704,67.43270038520576,49,2,1,5,39.881539777528005,60.79215500623491,50,2,0,14,29.36789829433681,64.11242769572034]}";

    private const string ShortStringsSav = @"{'Metadata':{'Bias':100,'Cases':0,'HeaderCodePage':65001,'DataCodePage':65001,'Variables':[
{'Name':'a\uD83E\uDD13\uD83E\uDD13','FormatType':1,'SpssWidth':32767,'DecimalPlaces':0,'Label':null,'ValueLabels':null,'MissingValueType':0,'MissingValues':[],'Columns':8,'Alignment':0,'MeasurementType':1},
{'Name':'a\uD83E\uDD13\uD83E\uDD131','FormatType':1,'SpssWidth':32767,'DecimalPlaces':0,'Label':null,'ValueLabels':null,'MissingValueType':0,'MissingValues':[],'Columns':8,'Alignment':0,'MeasurementType':1},
{'Name':'a\uD83E\uDD13\uD83E\uDD132','FormatType':1,'SpssWidth':32767,'DecimalPlaces':0,'Label':null,'ValueLabels':null,'MissingValueType':0,'MissingValues':[],'Columns':8,'Alignment':0,'MeasurementType':1},
{'Name':'a\uD83E\uDD13\uD83E\uDD133','FormatType':1,'SpssWidth':32767,'DecimalPlaces':0,'Label':null,'ValueLabels':null,'MissingValueType':0,'MissingValues':[],'Columns':8,'Alignment':0,'MeasurementType':1},
{'Name':'a\uD83E\uDD13\uD83E\uDD134','FormatType':1,'SpssWidth':32767,'DecimalPlaces':0,'Label':null,'ValueLabels':null,'MissingValueType':0,'MissingValues':[],'Columns':8,'Alignment':0,'MeasurementType':1},
{'Name':'a\uD83E\uDD13\uD83E\uDD135','FormatType':1,'SpssWidth':32767,'DecimalPlaces':0,'Label':null,'ValueLabels':null,'MissingValueType':0,'MissingValues':[],'Columns':8,'Alignment':0,'MeasurementType':1},
{'Name':'a\uD83E\uDD13\uD83E\uDD136','FormatType':1,'SpssWidth':32767,'DecimalPlaces':0,'Label':null,'ValueLabels':null,'MissingValueType':0,'MissingValues':[],'Columns':8,'Alignment':0,'MeasurementType':1},
{'Name':'a\uD83E\uDD13\uD83E\uDD137','FormatType':1,'SpssWidth':32767,'DecimalPlaces':0,'Label':null,'ValueLabels':null,'MissingValueType':0,'MissingValues':[],'Columns':8,'Alignment':0,'MeasurementType':1},
{'Name':'a\uD83E\uDD13\uD83E\uDD138','FormatType':1,'SpssWidth':32767,'DecimalPlaces':0,'Label':null,'ValueLabels':null,'MissingValueType':0,'MissingValues':[],'Columns':8,'Alignment':0,'MeasurementType':1},
{'Name':'a\uD83E\uDD13\uD83E\uDD139','FormatType':1,'SpssWidth':32767,'DecimalPlaces':0,'Label':null,'ValueLabels':null,'MissingValueType':0,'MissingValues':[],'Columns':8,'Alignment':0,'MeasurementType':1}]},'Data':[]}";


    [Fact]
    public void ShouldReadTestSav()
    {
        // Assign
        var fileStream = new FileStream("TestFiles/test.sav", FileMode.Open);

        // Act
        var spssData = SpssReader.Read(fileStream);

        // Assert
        var result = JsonSerializer.Serialize(spssData).Replace("\"", "'");
        Assert.Equal(ReadTestSav.Replace(NewLine, string.Empty), result);
    }

    [Fact]
    public void ShouldReadMissingValuesSav()
    {
        // Assign
        var fileStream = new FileStream("TestFiles/MissingValues.sav", FileMode.Open, FileAccess.Read, FileShare.Read, 2048 * 10, FileOptions.SequentialScan);

        // Act
        var spssData = SpssReader.Read(fileStream);

        // Assert
        var result = JsonSerializer.Serialize(spssData).Replace("\"", "'");
        Assert.Equal(MissingValuesSav.Replace(NewLine, string.Empty), result);
    }

    [Fact]
    public void ShouldReadCakeSpss1000SimilarvarsSav()
    {
        // Assign
        var fileStream = new FileStream("TestFiles/cakespss1000similarvars.sav", FileMode.Open, FileAccess.Read, FileShare.Read, 2048 * 10, FileOptions.SequentialScan);

        // Act
        var spssData = SpssReader.Read(fileStream);

        // Assert
        var result = JsonSerializer.Serialize(spssData).Replace("\"", "'");
        Assert.Equal(CakeSpss1000SimilarvarsSav.Replace(NewLine, string.Empty), result);
    }

    [Fact]
    public void ShouldReadLongstringSav()
    {
        // Assign
        var fileStream = new FileStream("TestFiles/Longstring.sav", FileMode.Open, FileAccess.Read, FileShare.Read, 2048 * 10, FileOptions.SequentialScan);

        // Act
        var spssData = SpssReader.Read(fileStream);

        // Assert
        var result = JsonSerializer.Serialize(spssData).Replace("\"", "'");
        Assert.Equal(LongstringSav.Replace(NewLine, string.Empty), result);
    }

    [Fact]
    public void ShouldReadShortStringsSav()
    {
        // Assign
        var fileStream = new FileStream("TestFiles/ShortStrings.sav", FileMode.Open, FileAccess.Read, FileShare.Read, 2048 * 10, FileOptions.SequentialScan);

        // Act
        var spssData = SpssReader.Read(fileStream);

        // Assert
        var result = JsonSerializer.Serialize(spssData).Replace("\"", "'");
        Assert.Equal(ShortStringsSav.Replace(NewLine, string.Empty), result);
    }

    [Fact]
    public void ShouldReadMaxStringSav()
    {
        // Assign
        var fileStream = new FileStream("TestFiles/MaxString.sav", FileMode.Open, FileAccess.Read, FileShare.Read);

        // Act
        var spssData = SpssReader.Read(fileStream);

        // Assert
        var result = JsonSerializer.Serialize(spssData).Replace("\"", "'");
        Assert.Equal(SavFiles.MaxStringSav.Replace(NewLine, string.Empty), result);
    }

    [Fact]
    public void ShouldReadBigEndianSav()
    {
        // Assign
        var fileStream = new FileStream("TestFiles/BigEndian.sav", FileMode.Open, FileAccess.Read, FileShare.Read);

        // Act
        var spssData = SpssReader.Read(fileStream);

        // Assert
        var result = JsonSerializer.Serialize(spssData).Replace("\"", "'");
        Assert.Equal(BigEndian.Replace(NewLine, string.Empty), result);
    }

    [Fact]
    public void ShouldReadLearningWithMultiLongStringMissingSav()
    {
        // Assign
        var fileStream = new FileStream("TestFiles/Learning.sav", FileMode.Open, FileAccess.Read, FileShare.Read);

        // Act
        var spssData = SpssReader.Read(fileStream);

        // Assert
        var result = JsonSerializer.Serialize(spssData).Replace("\"", "'");
        Assert.Equal(SavFiles.Learning.Replace(NewLine, string.Empty), result);
    }

    private void SampleReader()
    {
        using var fileStream = new FileStream("TestFiles/test.sav", FileMode.Open);

        var spssReader = new SpssReader(fileStream);

        var rowReader = spssReader.RowReader;
        foreach (var column in rowReader.Columns)
        {
            Console.WriteLine($"{column.Variable.Name}, {column.Variable.Label}");
            if (column.Variable.ValueLabels != null)
                Console.WriteLine(string.Join(",", column.Variable.ValueLabels.Select(x => $"{x.Key} - {x.Value} ")));
            Console.WriteLine(string.Join(",", column.Variable.MissingValues));
        }

        while (rowReader.ReadRow())
            foreach (var column in rowReader.Columns)
            {
                Console.WriteLine($"{column.Variable.Name}={column.GetValue()}");
                // OR without boxing
                switch (column.ColumnType)
                {
                    case ColumnType.String:
                        Console.WriteLine($"{column.Variable.Name}={column.GetString()}");
                        break;
                    case ColumnType.Double:
                        Console.WriteLine($"{column.Variable.Name}={column.GetDouble()}");
                        break;
                    case ColumnType.Int:
                        Console.WriteLine($"{column.Variable.Name}={column.GetInt()}");
                        break;
                    case ColumnType.Date:
                        Console.WriteLine($"{column.Variable.Name}={column.GetDate()}");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
    }
}