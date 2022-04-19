using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Spss.Encodings;
using Spss.FileStructure;
using Spss.MetadataWriters.Generators;
using Spss.MetadataWriters.RecordWriters;
using Spss.Models;
using Spss.SpssMetadata;

namespace Spss.MetadataWriters;

public class MetadataWriter
{
    private readonly List<DisplayParameter> _displayValues;
    private readonly Encoding _encoding;
    private readonly Metadata _metadata;
    private readonly RecordTypeInfoWriter _recordTypeInfoWriter;
    private readonly RecordTypeWriter _recordTypeWriter;
    private readonly List<ShortValueLabels> _shortValueLabels;
    private readonly List<VariableWrapper> _variables;

    public MetadataWriter(BinaryWriter writer, Metadata metadata)
    {
        _metadata = metadata;
        _variables = metadata.Variables.Select(x => new VariableWrapper(x)).ToList();
        _encoding = _encoding = Encoding.GetEncoding(metadata.HeaderCodePage, new RemoveReplacementCharEncoderFallback(), DecoderFallback.ReplacementFallback);

        ValidateVariables();
        new ShortNameGenerator(_encoding).GenerateShortNames(_variables);
        _shortValueLabels = ValueLabelIndexGenerator.GenerateLabelIndexes(_variables);
        _displayValues = DisplayValueGenerator.GenerateDisplayValues(_variables);
        _recordTypeWriter = new RecordTypeWriter(writer, _encoding);
        _recordTypeInfoWriter = new RecordTypeInfoWriter(writer, _encoding);
    }

    public void Write()
    {
        _recordTypeWriter.WriteHeaderRecord(_metadata, _variables);
        _recordTypeWriter.WriteVariableRecords(_variables);
        _recordTypeWriter.WriteValueLabelRecords(_shortValueLabels);
        _recordTypeInfoWriter.WriteMachineIntegerInfoRecord();
        _recordTypeInfoWriter.WriteMachineFloatingPointInfoRecord();
        _recordTypeInfoWriter.WriteDisplayValuesInfoRecord(_displayValues);
        _recordTypeInfoWriter.WriteLongVariableNamesRecord(_variables);
        _recordTypeInfoWriter.WriteVeryLongStringRecord(_variables);
        _recordTypeInfoWriter.WriteCharacterEncodingRecord();
        _recordTypeInfoWriter.WriteValueLabelStringRecords(_variables);
        _recordTypeInfoWriter.WriteMissingStringRecords(_variables);
        _recordTypeWriter.WriteDictionaryTerminationRecord();
    }

    public void ValidateVariables()
    {
        foreach (var variable in _variables)
        {
            variable.Name = TrimMaxLength(variable.Name, 64)!;
            variable.Label = TrimMaxLength(variable.Label, 254);

            if (variable.ValueLength > 32767) variable.ValueLength = 32767;

            if (variable.ValueLabels == null) return;

            foreach (var label in variable.ValueLabels) variable.ValueLabels[label.Key] = TrimMaxLength(label.Value, 120)!;
        }
    }

    private string? TrimMaxLength(string? name, int maxLength)
    {
        if (name == null) return null;

        var value = _encoding.GetTrimmedString(name, maxLength).Value;
        return value.Length < name.Length ? value : name;
    }
}