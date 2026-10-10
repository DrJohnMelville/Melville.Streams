using Melville.StreamInterfaces.Adapters;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces.Streams;

/// <summary>
/// Extension method for creating a Stream from an IStream Interface
/// </summary>
public static class IStreamExtension
{
    extension (IStream self)
    {
        /// <summary>
        /// Wrap an IStream in a C# stream
        /// </summary>
        /// <returns>The stream inteface wrapping the source IStream</returns>
        public Stream AsStream() => new InterfaceToStreamAdapter(self);
    }
}

/// <summary>
/// Base class for various adapter that wrap a stream and provide various capabilities.
/// </summary>
/// <param name="inner">The stream to be wrapped</param>
internal class InterfaceToStreamAdapter(IStream inner): Stream
{
    protected override void Dispose(bool disposing) => (inner as IDisposable)?.Dispose();
    public override ValueTask DisposeAsync() =>
        (inner as IAsyncDisposable)?.DisposeAsync() ?? ValueTask.CompletedTask;

    public override int Read(byte[] buffer, int offset, int count) => inner switch
    {
        ISyncReader sr => sr.Read(buffer, offset, count),
        IAsyncReader ar => ReadSyncFromAsyncMethods.Read(ar, buffer, offset, count),
        _ => throw new NotSupportedException("This stream does not support reading")
    };

    public override int Read(Span<byte> buffer) => inner switch
    {
        ISyncReader sr => sr.Read(buffer),
        IAsyncReader ar => ReadSyncFromAsyncMethods.Read(ar, buffer),
        _ => throw new NotSupportedException("This stream does not support reading")
    };

public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(new Memory<byte>(buffer).Slice(offset, count), cancellationToken).AsTask();
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return inner switch
        {
            IAsyncReader ar => ar.ReadAsync(buffer, cancellationToken),
            ISyncReader sr => new ValueTask<int>(Read(buffer.Span)),
            _ => throw new NotSupportedException("This stream does not support reading")
        };
    }
    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }
    public override void Write(byte[] buffer, int offset, int count)
    {
        switch (inner)
        {
            case ISyncWriter sw:
                sw.Write(buffer, offset, count);
                break;
            case IAsyncWriter aw:
                WriteSyncFromAsyncMethods.Write(aw, buffer, offset, count);
                break;
            default:
                throw new NotSupportedException("This stream does not support writing");
        }
    }
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        switch (inner)
        {
            case ISyncWriter sw:
                sw.Write(buffer);
                break;
            case IAsyncWriter aw:
                WriteSyncFromAsyncMethods.Write(aw, buffer);
                break;
            default:
                throw new NotSupportedException("This stream does not support writing");
        }
    }
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        switch (inner)
        {
            case IAsyncWriter aw:
                return aw.WriteAsync(buffer, cancellationToken);
            case ISyncWriter sw:
                if (!cancellationToken.IsCancellationRequested) sw.Write(buffer.Span);
                return ValueTask.CompletedTask;
            default:
                throw new NotSupportedException("This stream does not support writing");
        }
    }

    public override void Flush()
    {
    }

    public override void SetLength(long value)
    {
    }

    public override bool CanRead => inner is IAsyncReader or ISyncReader;

    public override bool CanSeek => inner is ISeekableStream;

    public override bool CanWrite => inner is IAsyncWriter or ISyncWriter;

    public override long Length => (inner as IStreamLength)?.Length ??
        throw new NotSupportedException("The underlying IStream does not report a length.");

    public override long Position
    {
        get => (inner as IStreamPosition)?.Position ??
            throw new NotImplementedException("The underlying IStream does not report a position.");

        set => Seek(value, SeekOrigin.Begin);
    }


}
