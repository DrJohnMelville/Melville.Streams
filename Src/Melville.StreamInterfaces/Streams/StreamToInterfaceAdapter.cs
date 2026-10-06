using Melville.IntersectionTypes;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces.Streams;

/// <summary>
/// Extension methods to wrap a Stream in an appropriat IStream interface.
/// 
/// These methods will verify that the underlying streams CanRead, CanWrite, and CanSeek properties
/// actually match the requested capabilities
/// </summary>
public static class StreamToInterfaceMethods
{
    extension(Stream inner)
    {
        /// <summary>
        /// Requests an IStream wrapping the current stream for syncrhonous and Asynchronous reading.
        /// </summary>
        public StreamReader AsStreamReader() => new(inner);
        /// <summary>
        /// Requests an IStream wrapping the current stream for syncrhonous and Asynchronous writing.
        /// </summary>
        public StreamWriter AsStreamWriter() => new(inner);
        /// <summary>
        /// Requests an IStream wrapping the current stream for syncrhonous and Asynchronous reading and writing.
        /// </summary>
        public StreamReaderWriter AsStreamReaderWriter() => new(inner);
        /// <summary>
        /// Requests an IStream wrapping the current stream for syncrhonous and Asynchronous reading and seeking.
        /// </summary>
        public SeekableStreamReader AsSeekableStreamReader() => new(inner);
        /// <summary>
        /// Requests an IStream wrapping the current stream for syncrhonous and Asynchronous writing and seeking.
        /// </summary>
        public SeekableStreamWriter AsSeekableStreamWriter() => new(inner);
        /// <summary>
        /// Requests an IStream wrapping the current stream for syncrhonous and Asynchronous reading and writing, and seeking.
        /// </summary>
        public SeekableStreamReaderWriter AsSeekableStreamReaderWriter() => new(inner);
    }
}

/// <summary>
/// Base class for the adaptors that wrap a Stream into an IStream type.
/// </summary>
public abstract class StreamToInterfaceAdapter :
    IStreamPosition, IStreamLength
{
    private readonly Stream inner;
    ///<inheritdoc/>
    public long Position => inner.Position;

    ///<inheritdoc/>
    public long Length => inner.Length;

    /// <summary>
    /// Create a StreamToInterface adapter, throwing if the required capabilities are not reported by
    /// the contained stream.
    /// </summary>
    /// <param name="inner">The stream to wrap.</param>
    /// <param name="canRead">Inner must be readable.</param>
    /// <param name="canWrite">Inner must be writable.</param>
    /// <param name="canSeek">Inner must be seekable.</param>
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
        ///<inheritdoc/>
        public int Read(Span<byte> buffer) => self.inner.Read(buffer);
        ///<inheritdoc/>
        public int Read(byte[] buffer, int position, int length) => self.inner.Read(buffer, position, length);

        ///<inheritdoc/>
        public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation = default) =>
            self.inner.ReadAsync(buffer, cancellation);
        ///<inheritdoc/>
        public ValueTask<int> ReadAsync(byte[] buffer, int position, int length, CancellationToken cancellation = default) =>
            new(self.inner.ReadAsync(buffer, position, length, cancellation));
    }

    internal readonly struct Seekers(StreamToInterfaceAdapter self): ISeekableStream
    {
        ///<inheritdoc/>
        public void Seek(long position) => self.inner.Seek(position, SeekOrigin.Begin);
        ///<inheritdoc/>
        public long Seek(long position, SeekOrigin origin) => self.inner.Seek(position, origin);
    }

    internal readonly struct Writers(StreamToInterfaceAdapter self): ISyncWriter, IAsyncWriter
    {
        ///<inheritdoc/>
        public void Write(ReadOnlySpan<byte> buffer) => self.inner.Write(buffer);
        ///<inheritdoc/>
        public void Write(byte[] source, int position, int length) => self.inner.Write(source, position, length);

        ///<inheritdoc/>
        public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellation = default) =>
            self.inner.WriteAsync(buffer, cancellation);

        ///<inheritdoc/>
        public ValueTask WriteAsync(byte[] buffer, int position, int length, CancellationToken cancellation = default) =>
            new(self.inner.WriteAsync(buffer, position, length, cancellation));
    }

}

/// <summary>
/// A wrapper class that allows reading from the underlying stream;
/// </summary>
/// <param name="inner">The stream to be wrapped</param>
public partial class StreamReader(Stream inner) : StreamToInterfaceAdapter(inner, true, false, false), 
    IMixin<StreamToInterfaceAdapter.Readers>
{

}

/// <summary>
/// A wrapper class that allows writing to the underlying stream;
/// </summary>
/// <param name="inner">The stream to be wrapped</param>
public partial class StreamWriter(Stream inner) : StreamToInterfaceAdapter(inner, false, true, false), 
    IMixin<StreamToInterfaceAdapter.Writers>
{

}

/// <summary>
/// A wrapper class that allows reading from or writing to the underlying stream;
/// </summary>
/// <param name="inner">The stream to be wrapped</param>
public partial class StreamReaderWriter(Stream inner) : StreamToInterfaceAdapter(inner, true, true, false), 
    IMixin<StreamToInterfaceAdapter.Readers>, IMixin<StreamToInterfaceAdapter.Writers>
{

}

/// <summary>
/// A wrapper class that allows seeking or reading from the underlying stream;
/// </summary>
/// <param name="inner">The stream to be wrapped</param>
public partial class SeekableStreamReader(Stream inner) : StreamToInterfaceAdapter(inner, true, false, true), 
    IMixin<StreamToInterfaceAdapter.Readers>, IMixin<StreamToInterfaceAdapter.Seekers>
{

}

/// <summary>
/// A wrapper class that allows seeking or writing to the underlying stream;
/// </summary>
/// <param name="inner">The stream to be wrapped</param>
public partial class SeekableStreamWriter(Stream inner) : StreamToInterfaceAdapter(inner, false, true, true), 
    IMixin<StreamToInterfaceAdapter.Writers>, IMixin<StreamToInterfaceAdapter.Seekers>
{

}

/// <summary>
/// A wrapper class that allows seeking,  reading from, or writing to the underlying stream;
/// </summary>
/// <param name="inner">The stream to be wrapped</param>
public partial class SeekableStreamReaderWriter(Stream inner) : StreamToInterfaceAdapter(inner, true, true, true), 
    IMixin<StreamToInterfaceAdapter.Readers>, IMixin<StreamToInterfaceAdapter.Writers>, IMixin<StreamToInterfaceAdapter.Seekers>
{

}