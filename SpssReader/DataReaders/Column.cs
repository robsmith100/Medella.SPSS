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
    private readonly IDataReader _dataReader;
    public readonly ColumnType ColumnType;
    private readonly int _spssWidth;
    private readonly byte[] _strArray = null!;
    private double? _doubleValue;
    private int? _intValue;
    private int _strLength;

    public Column(Variable variable, IDataReader dataReader)
    {
        Variable = variable;
        _dataReader = dataReader;
        _encoding = dataReader.DataEncoding;
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


    public Func<string?> GetString { get; internal set; } = () => throw new NotImplementedException();
    public Func<int?> GetInt { get; internal set; } = () => throw new NotImplementedException();
    public Func<double?> GetDouble { get; internal set; } = () => throw new NotImplementedException();
    public Func<DateTime?> GetDate { get; internal set; } = () => throw new NotImplementedException();

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal void ReadValue()
    {
        switch (ColumnType)
        {
            case ColumnType.String:
                _strLength = _dataReader.ReadString(_strArray.AsSpan(), _spssWidth);
                break;
            case ColumnType.Date:
            case ColumnType.Double:
            case ColumnType.Int:
                _dataReader.ReadNumber(out _doubleValue, out _intValue);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public object? GetValue()
    {
        return ColumnType switch
        {
            ColumnType.String => GetString(),
            ColumnType.Int => GetDouble(),
            ColumnType.Double => GetDouble(),
            ColumnType.Date => GetDate(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}

public enum ColumnType
{
    String,
    Double,
    Int,
    Date
}