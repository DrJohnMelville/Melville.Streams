using System;

namespace Melville.StreamInterfaces.Memory;

public class MemoryReader(ReadOnlyMemory<byte> store, long usedLength = -1) :
    MemoryReaderBase(usedLength, store.Length)
{
    protected override ReadOnlySpan<byte> Store => store.Span;
}
