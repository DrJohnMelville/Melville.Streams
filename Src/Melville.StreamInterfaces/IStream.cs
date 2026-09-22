using Melville.StreamInterfaces.Implementation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces;

public interface IStream
{
}

public interface ISyncReader: IStream
{
    int Read(Span<byte> buffer);
    int Read(byte[] buffer, int position, int length) => Read(buffer.AsSpan(position, length));
}

public interface IAsyncReader: IStream
{
    ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation = default);
    ValueTask<int> ReadAsync(byte[] buffer, int position, int length, CancellationToken cancellation = default) =>
        ReadAsync(buffer.AsMemory(position, length), cancellation);
}

public interface ISyncWriter: IStream
{
    void Write(ReadOnlySpan<byte> buffer);
    void Write(byte[] source, int position, int length) => Write(source.AsSpan(position, length));
}

public interface IAsyncWriter: IStream
{
    ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellation = default);
    ValueTask WriteAsync(byte[] buffer, int position, int length, CancellationToken cancellation = default) =>
        WriteAsync(buffer.AsMemory(position, length), cancellation);
}

public interface IStreamPosition: IStream
{
    long Position { get; }
}

public interface IStreamLength: IStream
{
    long Length { get; }
}

public static class OperationsForIStream
{
    extension (IStream self)
    {
        public long PositionOrException =>
          (self as IStreamPosition)?.Position ??
            throw new InvalidOperationException("Can only use SeekOrigin.Current in streams that implement IStreamPosition");
        public long LengthOrException =>
          (self as IStreamLength)?.Length ??
            throw new InvalidOperationException("Can only use SeekOrigin.End in streams that implement IStreamLength");
    }

    extension (Stream self)
    {
        public StreamToInterfaceAdapter AsStreamInterface() =>
            new StreamToInterfaceAdapter(self);
    }
}