# C# SPSS SAV file reader and writer library

[![Build Status](https://medella.visualstudio.com/Spss/_apis/build/status/Anderman.Medella.SPSS?branchName=master)](https://medella.visualstudio.com/Spss/_build/latest?definitionId=12&branchName=master)

[![Nuget](https://www.nuget.org/packages/SpssReader/)](https://img.shields.io/nuget/v/spsswriter)

![test](https://img.shields.io/azure-devops/tests/medella/spss/12)

![test](https://img.shields.io/azure-devops/coverage/medella/spss/12)

[Demo](https://spssreader.azurewebsites.net/api/Read) Spss to json convertor.

This library enables to read and write SPSS data files (.sav) on .net from and to a Stream. The library is UTF-8 safe.

It is available as a nuget package at https://www.nuget.org/packages/SpssReader en https://www.nuget.org/packages/SpssWriter, and can be installed using the package manager or by issueing:

```
Install-Package SpssReader
Install-Package SpssWriter
```

It's a fork of [SPSS-.NET-Reader](https://github.com/fbiagi/SPSS-.NET-Reader) by fbiagi (based on [spsslib-80132](http://spsslib.codeplex.com/) by elmarj).
Since forking there are a lot of bug fixing for utf8 support, added 
* string valuesLabels 
* string missing.
* BigEndian

The libriary is refactored to a cleancode library

### To read a data file:

```C#
    var fileStream = new FileStream("TestFiles/test.sav", FileMode.Open);

    var spssData = SpssReader.Read(fileStream);

    var variables = spssData.Metadata.Variables;
    foreach (var variable in variables)
    {
        Console.WriteLine($"{variable.Name}, {variable.Label}");
        if (variable.ValueLabels != null)
            Console.WriteLine(string.Join(",", variable.ValueLabels.Select(x => $"{x.Key} - {x.Value} ")));
        Console.WriteLine(string.Join(",", variable.MissingValues));
    }

    for (var i = 0; i < spssData.Data.Count; i++)
    {
        var obj = spssData.Data[i];
        Console.WriteLine($"{variables[i % variables.Count].Name}={obj}");
    }
}
```

### To write a data file:

```C#
// Create Variable list
    var variables = new List<Variable>
    {
        new Variable<string>("var0", 8)
        {
            Label = "label for var0",
            ValueLabels = new Dictionary<string, string>
            {
                ["a"] = "valueLabel for a",
                ["b"] = "valueLabel for b"
            }
        }.UserMissingValues(MissingValueType.OneDiscreteMissingValue, new[] { "-" }),
        new Variable<double>("var2", 7, 3)
        {
            Label = "label for var2",
            ValueLabels = new Dictionary<double, string>
            {
                [15.5] = "valueLabel for 15.5",
                [16] = "valueLabel for 16"
            }
        }.UserMissingValues(new[] { 0d }),
        new Variable<DateTime>("var4", 20)
        {
            Label = "label for var4",
            ValueLabels = new Dictionary<DateTime, string>
            {
                [new DateTime(2020, 1, 2)] = "valueLabel for 2 jan 2020",
                [new DateTime(2020, 1, 1)] = "valueLabel for 1 jan 2020"
            }
        }
    };
    // create data
    var data = new List<object?>
    {
        "string", 15.5, new DateTime(2020, 1, 1),
        null, null, null
    };

    //Write variable and data to a stream
    var ms = new MemoryStream();
    SpssWriter.Write(variables, data, ms);
```

If you find any bugs or have issues, please open an issue on GitHub.

## SAV file format

Binary description of `*.sav` file format is available here: http://www.gnu.org/software/pspp/pspp-dev/html_node/System-File-Format.html.
