using System;
using System.Threading;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces.Memory;

public class MemoryReaderWriter(Memory<byte> store, long usedLength = -1) :
     MemoryReaderBase(usedLength, store.Length), ISyncWriter, IAsyncWriter
{
    private Memory<byte> store = store;
    protected override ReadOnlySpan<byte> Store => store.Span;

    public void Write(ReadOnlySpan<byte> buffer)
    {
        TryExpandStream((int)buffer.Length);
        buffer.CopyTo(store.Span.Slice((int)Position));
        Position += buffer.Length;
        Length = Math.Max(Position, Length);
    }

    private void TryExpandStream(int length)
    {
        if (Position + length > store.Length)
        {
            var newBuffer = new byte[store.Length * 2];
            store.Span[..(int)Length].CopyTo(newBuffer);
            store = newBuffer;
        }
    }

    public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellation = default)
    {
        // do not potentially reallocate buffer for a cancelled write.
        if (!cancellation.IsCancellationRequested) Write(buffer.Span);
        return ValueTask.CompletedTask;
    }

    public Memory<byte> AsMemory() => store.Slice(0, (int)Length);
    public Span<byte> AsSpan() => AsMemory().Span;
}