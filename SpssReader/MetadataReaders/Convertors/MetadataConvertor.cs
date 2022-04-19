using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Spss.Models;
using Spss.SpssMetadata;

namespace Spss.MetadataReaders.Convertors;

public class MetadataConvertor
{
    private readonly Encoding _encoding;
    private readonly bool _isEndianCorrect;
    private readonly MetadataInfo _metadataInfo;

    public MetadataConvertor(MetadataInfo metadataInfo, bool isEndianCorrect)
    {
        _metadataInfo = metadataInfo;
        _isEndianCorrect = isEndianCorrect;
        _encoding = Encoding.GetEncoding(_metadataInfo.Metadata.HeaderCodePage);
    }

    public void Convert()
    {
        var variableList = _metadataInfo.Variables.Select(x => CreateVariable(_encoding.GetString(x.ShortName).TrimEnd(), _encoding.GetString(x.Label).TrimEnd(), x)).ToList();
        UpdateVariableValueLength(variableList);
        UpdateVariableDisplayParameters(variableList);
        var rawIndex2Index = _metadataInfo.Variables.Select((x, i) => (i, x.Index)).ToDictionary(x => x.Index, x => x.i);
        UpdateVariableShortValueLabels(variableList, rawIndex2Index);

        variableList = RemoveGhostVariable(variableList);
        var variables = variableList.ToDictionary(x => x.Name);
        UpdateVariableNames(variables);
        UpdateVariableLongValueLabels(variables);
        UpdateVariableLongStringMissing(variables);
        _metadataInfo.Metadata.Variables.AddRange(variables.Values);
    }

    private Variable CreateVariable(string shortName, string label, VariableProperties properties)
    {
        var variable = Variable.Create(shortName, label == string.Empty ? null : label, properties.FormatType, properties.SpssWidth, properties.DecimalPlaces);
        variable.MissingValueType = (MissingValueType)properties.MissingValueType;
        if (properties.Missing == null) return variable;

        variable.MissingValues = variable.FormatType == FormatType.A
            ? properties.Missing.Select(y => (object)_encoding.GetString(y).TrimEnd()).ToArray()
            : properties.Missing.Select(y => (object)BitConverter.ToDouble(y)).ToArray();
        return variable;
    }

    private void UpdateVariableLongStringMissing(Dictionary<string, Variable> variables)
    {
        var array = variables.Select(x => x.Value).ToDictionary(x => x.Name);
        foreach (var entry in _metadataInfo.LongStringMissing)
        {
            var v = entry.MissingValues.Select(valueBytes => (object)_encoding.GetString(valueBytes).TrimEnd()).ToArray();
            var name = _encoding.GetString(entry.VariableName).TrimEnd();
            array[name].MissingValues = v;
            array[name].MissingValueType = (MissingValueType)v.Length;
        }
    }

    private void UpdateVariableLongValueLabels(Dictionary<string, Variable> variables)
    {
        var array = variables.Select(x => x.Value).ToDictionary(x => x.Name);
        foreach (var entry in _metadataInfo.LongValueLabels)
        {
            var v = new Dictionary<object, string>();
            foreach (var (valueBytes, labelBytes) in entry.ValueLabels)
            {
                var value = _encoding.GetString(valueBytes).TrimEnd();
                var label = _encoding.GetString(labelBytes).TrimEnd();
                v[value] = label;
            }

            var name = _encoding.GetString(entry.VariableName).TrimEnd();
            array[name].ValueLabels = v;
        }
    }

    private void UpdateVariableShortValueLabels(List<Variable> variables, Dictionary<int, int> rawIndex2Index)
    {
        foreach (var entry in _metadataInfo.ShortValueLabels)
        {
            var indexes = entry.Indexes.Select(x => rawIndex2Index[x]).ToList();
            var isString = variables[indexes.First()].FormatType == FormatType.A;
            var valueLabels = entry.Labels;
            var v = new Dictionary<object, string>();
            foreach (var (valueBytes, labelBytes) in valueLabels)
            {
                var value = isString ? _encoding.GetString(valueBytes).TrimEnd() : (object)ConvertDouble(_isEndianCorrect, valueBytes);
                var label = _encoding.GetString(labelBytes).TrimEnd();
                v[value] = label;
            }

            indexes.ForEach(x => variables[x].ValueLabels = v);
        }
    }

    private void UpdateVariableDisplayParameters(List<Variable> variables)
    {
        for (var i = 0; i < _metadataInfo.DisplayParameters.Count; i++)
        {
            var parameter = _metadataInfo.DisplayParameters[i];
            variables[i].Alignment = parameter.Alignment;
            variables[i].Columns = parameter.Columns;
            variables[i].MeasurementType = parameter.Measure;
        }
    }

    private void UpdateVariableNames(Dictionary<string, Variable> variables)
    {
        if (_metadataInfo.LongVariableNames == null) return;

        var entries = _encoding.GetString(_metadataInfo.LongVariableNames).Split('\t');
        var longNames = entries.Select(x => x.Split('=')).Select(x => (shortName: x[0], longName: x[1])).ToList();
        longNames.ForEach(x => variables[x.shortName].Name = x.longName);
    }

    private void UpdateVariableValueLength(List<Variable> variables)
    {
        if (_metadataInfo.ValueLengthVeryLongString == null) return;

        var entries = _encoding.GetString(_metadataInfo.ValueLengthVeryLongString).Replace("\t", "").Split('\0', StringSplitOptions.RemoveEmptyEntries);
        var lengths = entries.Select(x => x.Split('=')).Select(x => (name: x[0], lentgh: int.Parse(x[1]))).ToDictionary(x => x.name, x => x.lentgh);
        foreach (var variable in variables)
            if (lengths.ContainsKey(variable.Name))
                variable.SpssWidth = lengths[variable.Name];
    }

    private static List<Variable> RemoveGhostVariable(List<Variable> variables)
    {
        var result = new List<Variable>();
        var skip = 0;
        foreach (var value in variables)
        {
            if (skip-- > 0) continue;

            result.Add(value);
            var length = value.SpssWidth;
            skip = length < 256 ? 0 : length / 252;
        }

        return result;
    }

    public static double ConvertDouble(bool isEndianCorrect, ReadOnlySpan<byte> bytes)
    {
        var result = MemoryMarshal.Read<long>(bytes);
        if (!isEndianCorrect) result = BinaryPrimitives.ReverseEndianness(result);

        var int64BitsToDouble = BitConverter.Int64BitsToDouble(result);
        return int64BitsToDouble;
    }
}