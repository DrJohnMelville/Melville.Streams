using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces.Streams;

public class StreamToInterfaceAdapter(Stream inner) : 
    ISyncReader, IAsyncReader, ISyncWriter, IAsyncWriter, ISeekableStream, IStreamPosition, IStreamLength
{

    public int Read(Span<byte> buffer) => inner.Read(buffer);
    public int Read(byte[] buffer, int position, int length) => inner.Read(buffer, position, length);


    public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation = default) =>
        inner.ReadAsync(buffer, cancellation);
    public ValueTask<int> ReadAsync(byte[] buffer, int position, int length, CancellationToken cancellation = default) =>
        new(inner.ReadAsync(buffer, position, length, cancellation));

    public void Seek(long position) => inner.Seek(position, SeekOrigin.Begin);
    public long Seek(long position, SeekOrigin origin) => inner.Seek(position, origin);

    public void Write(ReadOnlySpan<byte> buffer) => inner.Write(buffer);
    public void Write(byte[] source, int position, int length) => inner.Write(source, position, length);

    public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellation = default) =>
        inner.WriteAsync(buffer, cancellation);

    public ValueTask WriteAsync(byte[] buffer, int position, int length, CancellationToken cancellation = default) =>
        new(inner.WriteAsync(buffer, position, length, cancellation));

    public long Position => inner.Position;

    public long Length => inner.Length;
}
