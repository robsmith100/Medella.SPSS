using System;
using System.Runtime.CompilerServices;
using System.Text;
using Spss.FileStructure;
using Spss.SpssMetadata;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Spss.DataReaders;

public class Column
{
    private readonly Encoding _encoding;
    public readonly Variable Variable;
    private readonly Reader _reader;
    public readonly ColumnType ColumnType;
    private readonly int _spssWidth;
    private readonly byte[] _strArray = null!;
    private double? _doubleValue;
    private int? _intValue;
    private int _strLength;

    public Column(Variable variable, Reader reader)
    {
        Variable = variable;
        _reader = reader;
        _encoding = reader.DataEncoding;
        ReadValue = reader.CompressedType == CompressedType.Compressed ? ReadValueCompressed : ReadValueUnCompressed;
        _spssWidth = variable.SpssWidth;
        if (variable.FormatType == FormatType.A)
        {
            ColumnType = ColumnType.String;
            _strArray = new byte[variable.SpssWidth + 8];// Extra space for padding
            GetString = () => _strLength == 0 ? null : _encoding.GetString(_strArray.AsSpan()[.._strLength]);
            GetInt = () => TryGetString(out var str) && int.TryParse(str, out var value) ? value : null;
            GetDate = () => TryGetString(out var str) && DateTime.TryParse(str, out var value) ? value : null;
            GetDouble = () => TryGetString(out var str) && double.TryParse(str, out var value) ? value : null;
        }
        else if (variable.FormatType.IsDate())
        {
            ColumnType = ColumnType.Date;
            GetDate = () => (_doubleValue ?? _intValue)?.AsDate();
        }
        else if (variable.DecimalPlaces == 0)
        {
            ColumnType = ColumnType.Int;
            GetInt = () => _intValue ?? (_doubleValue == null ? null : (int)Math.Round(_doubleValue.Value, 0));
            GetDouble = () => _intValue ?? _doubleValue;
        }
        else
        {
            ColumnType = ColumnType.Double;
            GetInt = () => _intValue ?? (int?)_doubleValue;
            GetDouble = () => _doubleValue ?? _intValue;
        }

        bool TryGetString(out string? str)
        {
            if (_strLength == 0)
            {
                str = null;
                return false;
            }

            str = _encoding.GetString(_strArray.AsSpan()[.._strLength]);
            return true;
        }
    }


    internal Action ReadValue { get; set; }
    public Func<string?> GetString { get; internal set; } = () => throw new NotImplementedException();
    public Func<int?> GetInt { get; internal set; } = () => throw new NotImplementedException();
    public Func<double?> GetDouble { get; internal set; } = () => throw new NotImplementedException();
    public Func<DateTime?> GetDate { get; internal set; } = () => throw new NotImplementedException();

    private void ReadValueUnCompressed()
    {
        switch (ColumnType)
        {
            case ColumnType.String:
                _strLength = _reader.ReadString(_strArray.AsSpan(), _spssWidth);
                break;
            case ColumnType.Date:
            case ColumnType.Double:
            case ColumnType.Int:
                _doubleValue = _reader.ReadDouble();
                if (_doubleValue == double.MinValue) _doubleValue = null;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void ReadValueCompressed()
    {
        switch (ColumnType)
        {
            case ColumnType.String:
                _strLength = _reader.ReadStringCompressed(_strArray.AsSpan(), _spssWidth);
                break;
            case ColumnType.Date:
            case ColumnType.Double:
            case ColumnType.Int:
                _reader.ReadNumberCompressed(ref _doubleValue, ref _intValue);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public object? GetValue()
    {
        switch (ColumnType)
        {
            case ColumnType.String:
                return GetString();
            case ColumnType.Int:
            case ColumnType.Double:
                return GetDouble();
            //return GetInt();
            case ColumnType.Date:
                return GetDate();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

public enum ColumnType
{
    String,
    Double,
    Int,
    Date
}