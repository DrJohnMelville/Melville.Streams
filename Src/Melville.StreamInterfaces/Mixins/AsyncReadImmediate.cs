
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces.Mixins;

internal readonly struct AsyncReadImmediate(ISyncReader reader) : IAsyncReader
{
    ///<inheritdoc/>
    public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation = default) =>
        new(reader.Read(buffer.Span));
}

internal readonly partial struct AsyncWriteImmediate(ISyncWriter writer) : IAsyncWriter
{
    ///<inheritdoc/>
    public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellation = default)
    {
        // a cancelled write should not consume resources
        if (!cancellation.IsCancellationRequested) writer.Write(buffer.Span);
        return ValueTask.CompletedTask;
    }
}