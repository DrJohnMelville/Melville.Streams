using System;
using Melville.IntersectionTypes;
using Melville.StreamInterfaces.Mixins;

namespace Melville.StreamInterfaces.Memory;

public partial class MemoryReader(ReadOnlyMemory<byte> store, long usedLength = -1) :
    MemoryReaderBase(usedLength, store.Length)
{
    protected override ReadOnlySpan<byte> Store => store.Span;

}
