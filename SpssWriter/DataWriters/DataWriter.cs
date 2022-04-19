using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Spss.Encodings;
using Spss.FileStructure;
using Spss.SpssMetadata;

namespace Spss.DataWriters;

public class DataWriter
{
    private const int BlockSize = 8;

    // Compressed codes
    // 1 to 251 are the compressed values
    private readonly int _bias;
    private readonly List<object?> _data;
    private readonly Encoding _encoding;
    private readonly int _rows;
    private readonly List<Variable> _variables;
    private readonly BlockWriter _writer;
    private int _rowIndex;
    private int _variableIndex;

    public DataWriter(BinaryWriter writer, SpssData spssData)
    {
        _writer = new BlockWriter(writer);
        _variables = spssData.Metadata.Variables;
        _bias = spssData.Metadata.Bias;
        _rows = spssData.Metadata.Cases;
        _data = spssData.Data;
        _encoding = Encoding.GetEncoding(spssData.Metadata.DataCodePage, new RemoveReplacementCharEncoderFallback(), DecoderFallback.ReplacementFallback);
    }

    public void Write()
    {
        var rowLength = _variables.Count;
        for (var i = 0; i < _data.Count; i++)
        {
            var variable = _variables[i % rowLength];
            var formatType = variable.FormatType;
            var o = _data[i];
            if (formatType == FormatType.A)
            {
                if (o != null && o.GetType() != typeof(string))
                    WriteString($"{o} Expected string", variable.SpssWidth);
                else
                    WriteString((string)(o ?? string.Empty), variable.SpssWidth);
            }
            else if (formatType.IsDate())
            {
                InternalWrite((DateTime?)o);
            }
            else
            {
                InternalWrite((double?)o);
            }
        }
    }

    public void WriteSysMiss()
    {
        if (_variables[_variableIndex].FormatType != FormatType.A)
        {
            _writer.WriteCompressedCode(CompressedCode.SysMiss);
            UpdateVariableIndex();
            return;
        }

        Write(string.Empty);
    }

    public void Write(string? value)
    {
        if (_variables[_variableIndex].FormatType != FormatType.A) throw new InvalidOperationException($"Can't write string to a non string variable {_variables[_variableIndex].Name} ");

        WriteString(value ?? string.Empty, _variables[_variableIndex].SpssWidth); // string value can't have system missing
        UpdateVariableIndex();
    }

    public void Write(DateTime? value)
    {
        if (_variables[_variableIndex].FormatType == FormatType.A) throw new InvalidOperationException($"Can't write date to a string variable {_variables[_variableIndex].Name} ");

        InternalWrite(value);
        UpdateVariableIndex();
    }

    private void InternalWrite(DateTime? value)
    {
        var code = value == null ? CompressedCode.SysMiss : CompressedCode.Uncompressed;
        if (code == CompressedCode.Uncompressed) _writer.WriteToUncompressedBuffer(BitConverter.GetBytes(value!.Value.SpssDate()));

        _writer.WriteCompressedCode(code);
    }

    public void Write(double? value)
    {
        if (_variables[_variableIndex].FormatType == FormatType.A) throw new InvalidOperationException($"Can't write double to a string variable {_variables[_variableIndex].Name} ");

        InternalWrite(value);
        UpdateVariableIndex();
    }

    private void InternalWrite(double? value)
    {
        var code = GetUncompressedCode(value);
        if (code == CompressedCode.Uncompressed) _writer.WriteToUncompressedBuffer(BitConverter.GetBytes((double)value!));

        _writer.WriteCompressedCode(code);
    }

    public void Flush()
    {
        _writer.Flush();
    }

    private void UpdateVariableIndex()
    {
        _variableIndex++;
        if (_variables.Count != _variableIndex) return;

        _variableIndex = 0;
        _rowIndex++;
        if (_rows == _rowIndex) Flush();
    }

    private void WriteString(string value, int valueLength)
    {
        var rawVariableLength = SpssMath.GetAllocatedSize(valueLength);
        var bytes = _encoding.GetBytes(value.TrimEnd(), valueLength);
        var byteLength = bytes.Length;
        WriteStringBytes(bytes);
        _writer.AddPadding();
        var writtenBytes = byteLength / 255 * 256 + (byteLength % 255 + 7) / 8 * 8;

        while (writtenBytes < rawVariableLength)
        {
            _writer.WriteCompressedCode(CompressedCode.SpaceCharsBlock);
            writtenBytes += 8;
        }
    }

    private void WriteStringBytes(byte[] bytes)
    {
        var srcOffset = 0;
        var length = bytes.Length;
        var blockIndex = 0;
        while (srcOffset < length)
        {
            var insertSpace = (blockIndex + 8) % 256 == 0;
            var partLength = Math.Min(length - srcOffset, BlockSize - (insertSpace ? 1 : 0));
            _writer.WriteUncompressedBytes(bytes, srcOffset, partLength);
            srcOffset += partLength;
            blockIndex += partLength;
            if (blockIndex % 255 != 0) continue;

            _writer.AddSpace();
            blockIndex = 0;
        }
    }

    private byte GetUncompressedCode(double? value)
    {
        return value is null
            ? CompressedCode.SysMiss
            : Math.Abs(value.Value % 1) < 0.00001 && value + _bias > CompressedCode.Padding && value + _bias < CompressedCode.EndOfFile
                ? (byte)(value + _bias)
                : CompressedCode.Uncompressed;
    }
}