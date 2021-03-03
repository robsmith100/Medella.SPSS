using System.Collections.Generic;

namespace Spss.FileStructure
{
    public static class MachineFloatingPointInfo
    {
        private const ulong SystemMissingValue = 0xFFEFFFFFFFFFFFFFUL;
        private const ulong MissingHighestValue = 0x7FEFFFFFFFFFFFFFUL;
        private const ulong MissingLowestValue = 0xFFEFFFFFFFFFFFFEUL;
        public static readonly List<ulong> Items = new() { SystemMissingValue, MissingHighestValue, MissingLowestValue };
    }
}