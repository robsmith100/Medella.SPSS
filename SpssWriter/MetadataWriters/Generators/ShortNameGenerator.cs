using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spss.Encodings;
using Spss.FileStructure;
using Spss.Models;

// ReSharper disable StringLiteralTypo

namespace Spss.MetadataWriters.Generators
{
    public class ShortNameGenerator
    {
        private const string SuffixChar = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private readonly Encoding _encoding;
        private readonly HashSet<byte[]> _shortNames;

        public ShortNameGenerator(Encoding encoding)
        {
            _encoding = encoding;
            _shortNames = new HashSet<byte[]>(new ByteArrayEqualityComparer());
        }

        public void GenerateShortNames(List<VariableWrapper> records)
        {
            GenerateVariableShortNames(records);
            GenerateVariableGhostNames(records);
        }

        private void GenerateVariableShortNames(List<VariableWrapper> records)
        {
            for (var i = 0; i < records.Count; i++)
            {
                var record = records[i];
                var shortName = GetShortNameByteArray(record.Name);
                var i1 = i;
                record.ShortName8Bytes = GetUniqueShortName(shortName, index => $"V{i1}_{SuffixChar[index]}");
                record.ShortName = _encoding.GetString(record.ShortName8Bytes).TrimEnd();
            }
        }

        private void GenerateVariableGhostNames(List<VariableWrapper> records)
        {
            foreach (var record in records)
            {
                if (record.ValueLength < 256) continue;
                var ghostVariables = SpssMath.GetNumberOfGhostVariables(record.ValueLength);
                record.LastGhostVariableLength = record.ValueLength % 252;
                for (var j = 0; j < ghostVariables; j++)
                {
                    var sn = _encoding.GetString(GetShortNameByteArray(record.Name)).TrimEnd(); const int maxLengthPrefix = 5;
                    var prefix = _encoding.GetString(sn, maxLengthPrefix);
                    var shortName = _encoding.GetPaddedValueAsByteArray(prefix + '0', 8);
                    record.GhostNames.Add(GetUniqueShortName(shortName, index => $"{prefix}{GetGhostSuffix(index)}"));
                }
            }
        }

        private static string GetGhostSuffix(int index)
        {
            var chars = index < 36 ? 1 : index < 36 * 36 ? 2 : 3;
            var result = new char[chars];

            do
            {
                var remainder = index % 36;
                index /= 36;
                result[--chars] = remainder <= 9 ? (char) (remainder + '0') : (char) (remainder + '7');
            } while (chars > 0);

            return new string(result);
        }

        private byte[] GetUniqueShortName(byte[] shortName, Func<int, string> template)
        {
            if (_shortNames.Add(shortName))
                return shortName;
            var i = 0;
            do
            {
                shortName = _encoding.GetPaddedValueAsByteArray(template(i++), 8);
            } while (!_shortNames.Add(shortName));

            return shortName;
        }

        private byte[] GetShortNameByteArray(string? name) =>
            name == null ? new byte[8] : _encoding.GetPaddedValueAsByteArray(name.ToUpperInvariant(), 8);
    }

    public class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[]? x, byte[]? y) => x!.SequenceEqual(y!);
        public int GetHashCode(byte[] obj) => obj.Aggregate(13 * obj.Length, (current, b) => 17 * current + b);
    }
}