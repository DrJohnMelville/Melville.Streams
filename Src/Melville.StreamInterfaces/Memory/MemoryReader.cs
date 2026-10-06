using System;
using Melville.IntersectionTypes;
using Melville.StreamInterfaces.Mixins;

namespace Melville.StreamInterfaces.Memory;

/// <summary>
/// A stream reader implementation that uses a Memory&lt;byte&gt; as its data source.
/// </summary>
/// <param name="store">The data being read.</param>
/// <param name="usedLength">The amount of the store that is currently used</param>
public partial class MemoryReader(ReadOnlyMemory<byte> store, long usedLength = -1) :
    MemoryReaderBase(usedLength, store.Length)
{
    /// <inheritdoc/>
    protected override ReadOnlySpan<byte> Store => store.Span;

}
