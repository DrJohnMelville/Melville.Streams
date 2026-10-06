using Melville.IntersectionTypes;
using Melville.StreamInterfaces.Mixins;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces.Memory;


/// <summary>
/// Represents an IStream taking data from a Memory&lt;byte&gt;
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract partial class MemoryReaderBase : RandomAccessSeekableStreamBase,
     IStreamLength, ISyncReader, IMixin<AsyncReadImmediate>
{
    /// <summary>
    /// Create a MemoryReaderBase
    /// </summary>
    /// <param name="explicitLength">The length of used data in the buffer</param>
    /// <param name="bufferLen">The total length of the buffer.</param>
    /// <exception cref="ArgumentException"></exception>
    protected MemoryReaderBase(long explicitLength, int bufferLen)
    {
        if (explicitLength > bufferLen)
            throw new ArgumentException("Explicit length is greater than buffer length");
        Length = explicitLength >= 0 ? explicitLength : bufferLen;
    }

    /// <summary>
    /// Read only span derived from the underlying data source.  
    /// </summary>
    protected abstract ReadOnlySpan<byte> Store { get; }

    ///<inheritdoc/>
    public long Length { get; protected set; }

    ///<inheritdoc/>
    public int Read(Span<byte> buffer)
    {
        int len = Math.Min((int)(Length - Position), buffer.Length);
        Store.Slice((int)Position, len).CopyTo(buffer);
        return BumpPosition(len);
    }

    ///<inheritdoc/>
    public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation = default) =>
        ValueTask.FromResult(Read(buffer.Span));


    ///<inheritdoc/>
    public ReadOnlySpan<byte> AsReadOnlySpan() => Store[..(int)Length];
}
