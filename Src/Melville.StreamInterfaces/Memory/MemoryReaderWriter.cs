using Melville.IntersectionTypes;
using Melville.StreamInterfaces.Mixins;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces.Memory;

public partial class MemoryReaderWriter(Memory<byte> store, long usedLength = -1) :
     MemoryReaderBase(usedLength, store.Length), ISyncWriter, 
    IMixin<AsyncWriteImmediate>
{
    public MemoryReaderWriter():this((byte[])[], 0) { }

    private Memory<byte> store = store;
    protected override ReadOnlySpan<byte> Store => store.Span;

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

    public Memory<byte> AsMemory() => store.Slice(0, (int)Length);
    public Span<byte> AsSpan() => AsMemory().Span;
}