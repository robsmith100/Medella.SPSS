namespace Spss.FileStructure;

public enum RecordType
{
    HeaderRecord2 = 0x324C4624, // ASCII file header, in ascii chars: $FL2 sav
    HeaderRecord3 = 0x334C4624, // ASCII file header, in ascii chars: $FL3 zsav
    VariableRecord = 2,
    ValueLabelRecord = 3,
    ValueLabelVariablesRecord = 4,
    DocumentRecord = 6,
    InfoRecord = 7,
    EndRecord = 999
}