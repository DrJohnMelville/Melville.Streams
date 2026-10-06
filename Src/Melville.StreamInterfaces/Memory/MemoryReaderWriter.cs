using Melville.IntersectionTypes;
using Melville.StreamInterfaces.Mixins;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces.Memory;

/// <summary>
/// A stream that can read and write a memory bufer, expanding the buffer if necessary.
/// </summary>
/// <param name="store">Memory in which to store the stream data.</param>
/// <param name="usedLength">The amount of the buffer used with valid data.</param>
public partial class MemoryReaderWriter(Memory<byte> store, long usedLength = -1) :
     MemoryReaderBase(usedLength, store.Length), ISyncWriter, 
    IMixin<AsyncWriteImmediate>
{
    /// <summary>
    /// Construct an empty MemoryReaderWriter.
    /// </summary>
    public MemoryReaderWriter():this((byte[])[], 0) { }

    private Memory<byte> store = store;
    
    ///<inheritdoc/>
    protected override ReadOnlySpan<byte> Store => store.Span;

    ///<inheritdoc/>
    public void Write(ReadOnlySpan<byte> buffer)
    {
        TryExpandStream((int)buffer.Length);
        buffer.CopyTo(store.Span.Slice((int)Position));
        BumpPosition(buffer.Length);
        Length = Math.Max(Position, Length);
    }

    private void TryExpandStream(int length)
    {
        if (store.Length is 0)
        {
            store = new byte[Math.Max(32, length * 2)];
        }
        else if (Position + length > store.Length)
        {
            var newBuffer = new byte[store.Length * 2];
            store.Span[..(int)Length].CopyTo(newBuffer);
            store = newBuffer;
        }
    }

    /// <summary>
    /// The used portion of the backing store, as a Memory&lt;byte&gt;
    /// </summary>
    /// <returns></returns>
    public Memory<byte> AsMemory() => store.Slice(0, (int)Length);
    /// <summary>
    /// The used portion of the backing store, as a Span&lt;byte&gt;
    /// </summary>
    /// <returns></returns>
    public Span<byte> AsSpan() => AsMemory().Span;
}