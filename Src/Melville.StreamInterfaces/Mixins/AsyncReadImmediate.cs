
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces.Mixins;

public readonly struct AsyncReadImmediate(ISyncReader reader) : IAsyncReader
{
    public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation = default) =>
        new(reader.Read(buffer.Span));
}

public readonly partial struct AsyncWriteImmediate(ISyncWriter writer) : IAsyncWriter
{
    public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellation = default)
    {
        // a cancelled write should not consum resources
        if (!cancellation.IsCancellationRequested) writer.Write(buffer.Span);
        return ValueTask.CompletedTask;
    }
}