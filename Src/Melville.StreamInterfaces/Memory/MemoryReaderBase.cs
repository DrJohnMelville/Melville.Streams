using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces.Memory;


[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class MemoryReaderBase : RandomAccessSeekableStreamBase,
     IStreamLength, ISyncReader, IAsyncReader
{

    protected MemoryReaderBase(long explicitLength, int bufferLen)
    {
        if (explicitLength > bufferLen)
            throw new ArgumentException("Explicit length is greater than buffer length");
        Length = explicitLength >= 0 ? explicitLength : bufferLen;
    }
    protected abstract ReadOnlySpan<byte> Store { get; }

    public long Length { get; protected set; }

    public int Read(Span<byte> buffer)
    {
        int len = Math.Min((int)(Length - Position), buffer.Length);
        Store.Slice((int)Position, len).CopyTo(buffer);
        return BumpPosition(len);
    }

    public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation = default) =>
        ValueTask.FromResult(Read(buffer.Span));


    public ReadOnlySpan<byte> AsReadOnlySpan() => Store[..(int)Length];

}
