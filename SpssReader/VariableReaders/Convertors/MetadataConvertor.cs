using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spss.Models;
using SpssCommon.VariableModel;

namespace Spss.VariableReaders.Convertors
{
    public class MetadataConvertor
    {
        private readonly Encoding _encoding;
        private readonly MetadataInfo _metadataInfo;

        public MetadataConvertor(MetadataInfo metadataInfo)
        {
            _metadataInfo = metadataInfo;
            _encoding = Encoding.GetEncoding(_metadataInfo.Metadata.HeaderCodePage);
        }

        public void Convert()
        {
            var variables = _metadataInfo.Variables
                .Select(x => (shortName: _encoding.GetString(x.ShortName).TrimEnd(), label: _encoding.GetString(x.Label).TrimEnd(), v: x))
                .ToDictionary(x => x.shortName, x => CreateVariable(x.shortName, x.label, x.v));
            var rawIndex2Index = _metadataInfo.Variables.Select((x, i) => (i, x.Index)).ToDictionary(x => x.Index, x => x.i);
            UpdateVariableValueLength(variables);
            UpdateVariableNames(variables);
            UpdateVariableDisplayParameters(variables);
            UpdateVariableShortValueLabels(variables, rawIndex2Index);
            UpdateVariableLongValueLabels(variables);
            UpdateVariableLongStringMissing(variables);
            _metadataInfo.Metadata.Variables = variables.Values.ToList();
        }

        private Variable CreateVariable(string shortName, string label, VariableProperties properties)
        {
            var variable = Variable.Create(shortName, label == string.Empty ? null : label, properties.FormatType, properties.SpssWidth, properties.DecimalPlaces);
            variable.MissingValueType = (MissingValueType) properties.MissingValueType;
            if (properties.Missing == null)
                return variable;
            variable.MissingValues = variable.FormatType == FormatType.A
                ? properties.Missing.Select(y => (object) _encoding.GetString(y).TrimEnd()).ToArray()
                : properties.Missing.Select(y => (object) BitConverter.ToDouble(y)).ToArray();
            return variable;
        }

        private void UpdateVariableLongStringMissing(Dictionary<string, Variable> variables)
        {
            var array = variables.Select(x => x.Value).ToDictionary(x => x.Name);
            foreach (var entry in _metadataInfo.LongStringMissing)
            {
                var v = entry.MissingValues.Select(valueBytes => (object) _encoding.GetString(valueBytes).TrimEnd()).ToArray();
                var name = _encoding.GetString(entry.VariableName).TrimEnd();
                array[name].MissingValues = v;
                array[name].MissingValueType = (MissingValueType) v.Length;
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

        private void UpdateVariableShortValueLabels(Dictionary<string, Variable> variables, Dictionary<int, int> rawIndex2Index)
        {
            var array = variables.Select(x => x.Value).ToArray();
            foreach (var entry in _metadataInfo.ShortValueLabels)
            {
                var indexes = entry.Indexes.Select(x => rawIndex2Index[x]).ToList();
                var isString = array[indexes.First()].FormatType == FormatType.A;
                var valueLabels = entry.Labels;
                var v = new Dictionary<object, string>();
                foreach (var (valueBytes, labelBytes) in valueLabels)
                {
                    var value = isString ? _encoding.GetString(valueBytes).TrimEnd() : (object) BitConverter.ToDouble(valueBytes);
                    var label = _encoding.GetString(labelBytes).TrimEnd();
                    v[value] = label;
                }

                indexes.ForEach(x => array[x].ValueLabels = v);
            }
        }

        private void UpdateVariableDisplayParameters(Dictionary<string, Variable> variables)
        {
            var array = variables.Select(x => x.Value).ToArray();
            for (var i = 0; i < _metadataInfo.DisplayParameters.Count; i++)
            {
                var parameter = _metadataInfo.DisplayParameters[i];
                array[i].Alignment = parameter.Alignment;
                array[i].Columns = parameter.Columns;
                array[i].MeasurementType = parameter.Measure;
            }
        }

        private void UpdateVariableNames(Dictionary<string, Variable> variables)
        {
            var entries = _encoding.GetString(_metadataInfo.LongVariableNames).Split('\t');
            var longNames = entries.Select(x => x.Split('=')).Select(x => (shortName: x[0], longName: x[1])).ToList();
            longNames.ForEach(x => variables[x.shortName].Name = x.longName);
        }

        private void UpdateVariableValueLength(Dictionary<string, Variable> variables)
        {
            if (_metadataInfo.ValueLengthVeryLongString == null) return;
            var entries = _encoding.GetString(_metadataInfo.ValueLengthVeryLongString).Replace("\t", "").Split('\0', StringSplitOptions.RemoveEmptyEntries);
            var lengths = entries.Select(x => x.Split('=')).Select(x => (name: x[0], lentgh: int.Parse(x[1]))).ToList();
            lengths.ForEach(x => variables[x.name].SpssWidth = x.lentgh);
        }
    }
}
