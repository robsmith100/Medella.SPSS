namespace Spss.FileStructure;

public static class InfoRecordType
{
    public const int MachineInteger = 0x03;
    public const int MachineFloatingPoint = 0x04;
    public const int GroupedVariables = 5;
    public const int DateInfo = 6;
    public const int MultipleResponseSets = 7;
    public const int ProductInfo = 0x0A;
    public const int VariableDisplayParameter = 0x0B;
    public const int LongVariableNames = 0x0D;
    public const int ValueLengthVeryLongString = 0x0E;
    public const int ExtendedNumberOfCases = 0x10;
    public const int DataFileAttributes = 17;
    public const int VariableAttributes = 18;
    public const int MultipleResponseSetsV14 = 19;
    public const int CharacterEncoding = 0x14;
    public const int LongStringValueLabels = 0x15;
    public const int LongStringMissing = 0x16;
}