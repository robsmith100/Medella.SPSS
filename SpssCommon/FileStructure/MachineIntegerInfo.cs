using System.Collections.Generic;

namespace Spss.FileStructure;

public class MachineIntegerInfo
{
    private const int VersionMajor = 0x19;
    private const int VersionMinor = 0;
    private const int VersionRevision = 0;
    private const int MachineCode = -1;
    private const int FloatingPointRepresentation = 1;
    private const int CompressionCode = 1;
    private const int Endianness = 2;
    private const int CharacterCode = 65001; //codepage for utf-8
    public static readonly List<int> Items = new() { VersionMajor, VersionMinor, VersionRevision, MachineCode, FloatingPointRepresentation, CompressionCode, Endianness, CharacterCode };
}