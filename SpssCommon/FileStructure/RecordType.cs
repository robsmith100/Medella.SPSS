namespace SpssCommon.FileStructure
{
    public enum RecordType
    {
        HeaderRecord = 0x324C4624, // ASCII file header, in ascii chars: $FL2 
        VariableRecord = 2,
        ValueLabelRecord = 3,
        ValueLabelVariablesRecord = 4,
        DocumentRecord = 6,
        InfoRecord = 7,
        EndRecord = 999
    }
}