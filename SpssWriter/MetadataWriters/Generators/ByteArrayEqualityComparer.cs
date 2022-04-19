using System.Collections.Generic;
using System.Linq;

namespace Spss.MetadataWriters.Generators;

public class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
{
    public bool Equals(byte[]? x, byte[]? y)
    {
        return x!.SequenceEqual(y!);
    }

    public int GetHashCode(byte[] obj)
    {
        return obj.Aggregate(13 * obj.Length, (current, b) => 17 * current + b);
    }
}