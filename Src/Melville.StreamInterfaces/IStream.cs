using Melville.StreamInterfaces.Streams;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces;

public interface IStream
{
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
        public StreamToInterfaceAdapter AsReaderWriter() =>
            new StreamToInterfaceAdapter(self);
    }
}
