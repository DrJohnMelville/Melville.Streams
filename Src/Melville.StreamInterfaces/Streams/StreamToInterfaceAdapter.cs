using Melville.IntersectionTypes;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces.Streams;

public static class StreamToInterfaceMethods
{
    extension(Stream inner)
    {
        public StreamReader AsStreamReader() => new(inner);
        public StreamWriter AsStreamWriter() => new(inner);
        public StreamReaderWriter AsStreamReaderWriter() => new(inner);
        public SeekableStreamReader AsSeekableStreamReader() => new(inner);
        public SeekableStreamWriter AsSeekableStreamWriter() => new(inner);
        public SeekableStreamReaderWriter AsSeekableStreamReaderWriter() => new(inner);
    }
}
public abstract class StreamToInterfaceAdapter :
    IStreamPosition, IStreamLength
{
    private readonly Stream inner;
    public long Position => inner.Position;

    public long Length => inner.Length;

    protected StreamToInterfaceAdapter(Stream inner, bool canRead, bool canWrite, bool canSeek)
    {
        VerifyAccess(canRead, inner.CanRead, "Stream is not readable, but used to construct a reader.");
        VerifyAccess(canWrite, inner.CanWrite, "Stream is not writable, but used to construct a writer.");
        VerifyAccess(canSeek, inner.CanSeek, "Stream is not seekable, but used to construct a seekable reader or writer.");
        this.inner = inner;
    }

    void VerifyAccess(bool requested, bool supported, string errorMessage)
    {
        if (requested && !supported)
            throw new InvalidOperationException(errorMessage);
    }

    internal readonly struct Readers(StreamToInterfaceAdapter self): ISyncReader, IAsyncReader
    {
        public int Read(Span<byte> buffer) => self.inner.Read(buffer);
        public int Read(byte[] buffer, int position, int length) => self.inner.Read(buffer, position, length);

        public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation = default) =>
            self.inner.ReadAsync(buffer, cancellation);
        public ValueTask<int> ReadAsync(byte[] buffer, int position, int length, CancellationToken cancellation = default) =>
            new(self.inner.ReadAsync(buffer, position, length, cancellation));
    }

    internal readonly struct Seekers(StreamToInterfaceAdapter self): ISeekableStream
    {
        public void Seek(long position) => self.inner.Seek(position, SeekOrigin.Begin);
        public long Seek(long position, SeekOrigin origin) => self.inner.Seek(position, origin);
    }

    internal readonly struct Writers(StreamToInterfaceAdapter self): ISyncWriter, IAsyncWriter
    {
        public void Write(ReadOnlySpan<byte> buffer) => self.inner.Write(buffer);
        public void Write(byte[] source, int position, int length) => self.inner.Write(source, position, length);

        public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellation = default) =>
            self.inner.WriteAsync(buffer, cancellation);

        public ValueTask WriteAsync(byte[] buffer, int position, int length, CancellationToken cancellation = default) =>
            new(self.inner.WriteAsync(buffer, position, length, cancellation));
    }

}

public partial class StreamReader(Stream inner) : StreamToInterfaceAdapter(inner, true, false, false), 
    IMixin<StreamToInterfaceAdapter.Readers>
{

}

public partial class StreamWriter(Stream inner) : StreamToInterfaceAdapter(inner, false, true, false), 
    IMixin<StreamToInterfaceAdapter.Writers>
{

}
public partial class StreamReaderWriter(Stream inner) : StreamToInterfaceAdapter(inner, true, true, false), 
    IMixin<StreamToInterfaceAdapter.Readers>, IMixin<StreamToInterfaceAdapter.Writers>
{

}

public partial class SeekableStreamReader(Stream inner) : StreamToInterfaceAdapter(inner, true, false, true), 
    IMixin<StreamToInterfaceAdapter.Readers>, IMixin<StreamToInterfaceAdapter.Seekers>
{

}

public partial class SeekableStreamWriter(Stream inner) : StreamToInterfaceAdapter(inner, false, true, true), 
    IMixin<StreamToInterfaceAdapter.Writers>, IMixin<StreamToInterfaceAdapter.Seekers>
{

}
public partial class SeekableStreamReaderWriter(Stream inner) : StreamToInterfaceAdapter(inner, true, true, true), 
    IMixin<StreamToInterfaceAdapter.Readers>, IMixin<StreamToInterfaceAdapter.Writers>, IMixin<StreamToInterfaceAdapter.Seekers>
{

}