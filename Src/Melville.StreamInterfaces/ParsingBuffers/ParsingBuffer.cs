using Melville.INPC;
using Melville.StreamInterfaces.Adapters;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces.ParsingBuffers;
/// <summary>
/// Implements a buffer that can be incrementally filled from a stream and used for parsing.
/// </summary>
/// <param name="source">The IAsyncReader to get data from.</param>
/// <param name="initialBufferSize">The desired initial size of the buffer, opr 0 for a default value</param>
public partial class ParsingBuffer(IAsyncReader source, int initialBufferSize = 0) : IDisposable
{
    #region buffer creation and disposal
    private byte[] buffer = ArrayPool<byte>.Shared.Rent(DesiredBufferLength(source, initialBufferSize));

    private const int defaultBufferSize = 8 * 1024;
    private static int DesiredBufferLength(IStream source, int initialBufferSize) =>
        (initialBufferSize > 1) ? initialBufferSize : BufferSizeToContainStream(source);

    private static int BufferSizeToContainStream(IStream source) =>
        source.BytesLeft() switch
        {
            < 1 or > defaultBufferSize => defaultBufferSize,
            var measuredBytes => (int)measuredBytes
        };

    ///<inheritdoc/>
    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(buffer);
        buffer = [];
    }
    #endregion

    #region Used portions of buffer
    private int firstByte;
    private int nextByteSpace;
    /// <summary>
    /// A span of the current parsable data.
    /// </summary>    /// <returns></returns>
    public Span<byte> Peek() => buffer.AsSpan(firstByte, CurrentLength);

    /// <summary>
    /// Peek at the unread buffer up to a given number of bytes
    /// </summary>
    /// <param name="bytes">Maximum number of bytes to return</param>
    public Span<byte> Peek(int bytes) => Peek()[..Math.Min(bytes, CurrentLength)];


    /// <summary>
    /// A memory of the current parsable data
    /// </summary>
    /// <returns></returns>
    public Memory<byte> PeekMemory() => buffer.AsMemory(firstByte, CurrentLength);

    /// <summary>
    /// Peek at the unread buffer up to a given number of bytes
    /// </summary>
    /// <param name="bytes">Maximum number of bytes to return</param>
    public Memory<byte> PeekMemory(int bytes) => PeekMemory()[..Math.Min(bytes, CurrentLength)];


    ///<inheritdoc/>
    public int CurrentLength => nextByteSpace - firstByte;

    ///<inheritdoc/>
    public long RelativePosition { get; set; }

    /// <summary>
    /// Consume the given number of bytes in the parsing buffer -- allowing them to be overwritten
    /// </summary>
    /// <param name="bytes">The number of bytes to consume.</param>
    /// <exception cref="InvalidOperationException">If bytes is larger than the remaining length of the
    /// parsing buffer.</exception>
    public void Advance(int bytes)
    {
        firstByte += bytes;
        if ((uint)firstByte > nextByteSpace)
            throw new InvalidOperationException("Claimed negative or more bytes than the buffer actually contains");
        RelativePosition += bytes;
    }

    /// <summary>
    /// Skip forward to the given relative position.
    /// </summary>
    /// <param name="newRelativePosition">The desired next relative position.</param>
    /// <param name="ct">The cancellation token for the operation</param>
    /// <exception cref="InvalidOperationException">The desired relative position is less than the current position
    /// </exception>
    public ValueTask AdvanceToRelativePositionAsync(long newRelativePosition, CancellationToken ct = default) =>
        AdvanceAsync((int)(newRelativePosition - RelativePosition), ct);

    /// <summary>
    /// Advance the reader forward by a given number of bytes, reading additional data from the source stream
    /// if necessary to do so.  If the underlying stream can seek relative to the current position, the
    /// method may use a Seek call to quickly advance the underlying stream.  If the underlying stream cannot seek
    /// will repeatedly read the stream to advance the right number of bytes.
    /// </summary>
    /// <param name="delta"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public ValueTask AdvanceAsync(int delta, CancellationToken ct = default)
    {
        if (TryLocalAdvance(ref delta)) return ValueTask.CompletedTask;
        if (source is ISeekableStream { CanSeekRelativeToPosition: true} seek)
        {
            seek.Seek(delta, SeekOrigin.Current);
            RelativePosition += delta;
            return ValueTask.CompletedTask;
        }
        else
            return ReadingAdvance(delta, ct);

    }

    bool TryLocalAdvance(ref int delta)
    {
        if (delta is 0) return true;
        var takeable = Math.Min(delta, CurrentLength);
        Advance(takeable);
        delta -= takeable;
        return delta <= 0;
    }

    async ValueTask ReadingAdvance(int delta, CancellationToken ct = default)
    {
        do
        {
            if ((await ReadBytesAsync(1, ct).CA()) is 0)
                throw new EndOfStreamException("Ran out of stream when advancing forward.");
        } while (!TryLocalAdvance(ref delta));
    }

    #endregion

    #region Read additional bytes into the buffer
    /// <summary>
    /// Attempt to ensure that the parsing buffer is at least a given size.
    /// </summary>
    /// <param name="desiredSize">The minimum number of bytes desired in the buffer.</param>
    /// <param name="ct">The cancellation token</param>
    /// <returns>The actual number of new bytes read.</returns>
    public ValueTask<int> TryEnsureBytesAsync(int desiredSize, CancellationToken ct = default) =>
        CurrentLength >= desiredSize ?
            ValueTask.FromResult(0) :
            ReadBytesAsync(desiredSize, ct);

    /// <summary>
    /// Ensure that the parsing buffer is at least a given size, or throw an exception if unable.
    /// </summary>
    /// <param name="desiredSize">The minimum number of bytes desired in the buffer.</param>
    /// <param name="ct">The cancellation token</param>
    /// <exception cref="EndOfStreamException"></exception>
    public ValueTask<int> EnsureBytesAsync(int desiredSize, CancellationToken ct = default) =>
        CurrentLength >= desiredSize ? //  on the hot path where the bytes are in memory avoid a state machine.
            ValueTask.FromResult(0) :
            InnerEnsureBytesAsync(desiredSize, ct);

    private async ValueTask<int> InnerEnsureBytesAsync(int desiredSize, CancellationToken ct = default)
    {
        var ret = await ReadBytesAsync(desiredSize).CA();
        if (CurrentLength < desiredSize && !ct.IsCancellationRequested)
            throw new EndOfStreamException("Not enough bytes for EnsureBytesAsync");
        return ret;
    }

    /// <summary>
    /// Unconditionally read additional bytes from the source.
    /// 
    /// Prior to reading this function may choose to roll data to the front of the buffer
    /// or expand the buffer if needed to read additional data.
    /// </summary>
    /// <param name="desiredSize">The minimum desired size of the buffer after the read.  This
    /// request may not be fulfilled if the stream runs out of data, but will repeatedly
    /// read the source stream to try and get the requested number of bytes.</param>
    /// <param name="ct">The cancellation token</param>
    /// <returns>Number of new bytes read into the buffer.</returns>
    public async ValueTask<int> ReadBytesAsync(int desiredSize = 0, CancellationToken ct = default)
    {
        TryCycleBuffer(desiredSize);
        var bytesNeeded = Math.Max(1, desiredSize - CurrentLength);
        return ProcessReadResult(
            await source.ReadAtLeastAsync(buffer.AsMemory(nextByteSpace), bytesNeeded, ct, false).CA());
    }

    private int ProcessReadResult(int ret)
    {
        nextByteSpace += ret;
        DoneReadingSource = ret is 0;
        return ret;
    }

    /// <summary>
    /// Synchronously try to read more data into the stream.  Uses ReadSyncFromAsyncMethods to readd the
    /// underlying stream.  As long as the underlying stream actually implements ISyncReader this is safe.
    /// Otherwise you get the risk of synchronously waiting on the threadpool.
    /// </summary>
    /// <returns>The number of bytes read into the buffer.</returns>
    public int DangerousReadBytesSync()
    {
        TryCycleBuffer(1);
        return ProcessReadResult(SyncWrapper.Read(buffer, nextByteSpace, buffer.Length - nextByteSpace));
        // Use the old array style read call because if we end up inside a SyncFromAsyncReader this call does
        // not have to allocate an external buffer to marshall buffer into the async call.
    }
    private ISyncReader SyncWrapper => field ??= source.AsISyncReader();

    /// <summary>
    /// The source IStream has no more data to read into the buffer.  This is if the last read returned 
    /// 0 bytes.
    /// </summary>
    public bool DoneReadingSource { get; private set; }

    void TryCycleBuffer(int desiredSize)
    {
        int bufferLength = buffer.Length;
        if (desiredSize > bufferLength)
        {
            ExpandBufferSizeMoreThan(desiredSize);
        }
        else if (CurrentLength == bufferLength)
        {
            ExpandBufferSizeMoreThan(bufferLength * 2);
        }
        else if (CurrentLength is 0)
        {
            ResetToNewBuffer();
        }
        else if (
            firstByte + desiredSize > bufferLength ||
            firstByte > bufferLength / 2 ||
            CurrentLength < 17)
        {
            CycleToFrontOfBuffer();
        }
    }
    void ExpandBufferSizeMoreThan(int newSize)
    {
        var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        Peek().CopyTo(newBuffer);
        ArrayPool<byte>.Shared.Return(buffer);
        buffer = newBuffer;
        SlideIndicesToFrontOfBuffer();
    }
    void ResetToNewBuffer() => firstByte = nextByteSpace = 0;
    void CycleToFrontOfBuffer()
    {
        Peek().CopyTo(buffer);
        SlideIndicesToFrontOfBuffer();
    }

    private void SlideIndicesToFrontOfBuffer()
    {
        // order of tne next two items is critical -- CurrentLength depends on both nextByteSpace and FirstByte
        nextByteSpace = CurrentLength;
        firstByte = 0;
    }
    #endregion

    #region FillExternalBuffer
    /// <summary>
    /// Read a block of data into an external buffer.  If the buffer is larger than data in the reader's buffer
    /// this method will avoid cycling data through the reader's buffer.  If there is not enough data, throw
    /// an exception.
    /// </summary>
    /// <param name="externalBuffer">The buffer to write data into.</param>
    /// <param name="ct">The cancellation token</param>
    /// <exception cref="EndOfStreamException">If there is not enough data in the stream to fill the buffer.</exception>
    public async ValueTask FillExternalBufferAsync(Memory<byte> externalBuffer, CancellationToken ct = default)
    {
        var read = await TryFillExternalBufferAsync(externalBuffer, ct);
        if (read < externalBuffer.Length && !ct.IsCancellationRequested)
            throw new EndOfStreamException("Not enough bytes tof fill buffer");
    }

    /// <summary>
    /// Try to fill an external buffer with bytes from the stream.
    /// </summary>
    /// <param name="externalBuffer">The external buffer to fill</param>
    /// <param name="ct">A cancellation token</param>
    /// <returns>The number of bytes filled in the buffer.</returns>
    public async ValueTask<int> TryFillExternalBufferAsync(Memory<byte> externalBuffer, CancellationToken  ct = default)
    {
        var inMemLength = Math.Min(externalBuffer.Length, CurrentLength);
        if (inMemLength > 0)
        {
            Peek()[..inMemLength].CopyTo(externalBuffer.Span);
            Advance(inMemLength);
        }
        return inMemLength == externalBuffer.Length ? inMemLength :
            inMemLength + await source.ReadExactAsync(externalBuffer[inMemLength..], ct, false);
    }
    #endregion

    #region NumberParsers
    /// <summary>
    /// Remove a group of bytes from the parsing buffer and then advance past them.  
    /// The resulting Memory&lt;byte&gt; is only guaranteed to be valid before the
    /// next ReadBytesAsync operation, which may reorganize the buffer.
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public async ValueTask<Memory<byte>> TakeBytesAsync(int bytes)
    {
        await EnsureBytesAsync(bytes);
        var ret = PeekMemory()[..bytes];
        Advance(bytes);
        return ret;
    }

    /// <summary>
    /// Read an unsigned byte from the buffer and advance past it.
    /// </summary>
    [MacroItem(16, 2)]
    [MacroItem(32, 4)]
    [MacroItem(64, 8)]
    [MacroItem(128, 16)]
    [MacroCode("""
          /// <summary>
          /// Read a big endian UInt~0~ from the buffer and advance past it.
          /// </summary>
          public async ValueTask<UInt~0~> GetUInt~0~BigEndianAsync() =>
               BinaryPrimitives.ReadUInt~0~BigEndian((await TakeBytesAsync(~1~)).Span);
          /// <summary>
          /// Read a little endian UInt~0~ from the buffer and advance past it.
          /// </summary>
                   public async ValueTask<UInt~0~> GetUInt~0~LittleEndianAsync() =>
               BinaryPrimitives.ReadUInt~0~LittleEndian((await TakeBytesAsync(~1~)).Span);
          /// <summary>
          /// Read a big endian Int~0~ from the buffer and advance past it.
          /// </summary>
                   public async ValueTask<Int~0~> GetInt~0~BigEndianAsync() =>
               BinaryPrimitives.ReadInt~0~BigEndian((await TakeBytesAsync(~1~)).Span);
          /// <summary>
          /// Read a little endian Int~0~ from the buffer and advance past it.
          /// </summary>
                   public async ValueTask<Int~0~> GetInt~0~LittleEndianAsync() =>
               BinaryPrimitives.ReadInt~0~LittleEndian((await TakeBytesAsync(~1~)).Span);
        """)]
    public async ValueTask<byte> GetUInt8(){
        await EnsureBytesAsync(1);
        var ret = Peek()[0];
        Advance(1);
        return ret;
    }

    /// <summary>
    /// Read a signed byte from the buffer and advance past it.
    /// </summary>
    public async ValueTask<sbyte> GetInt8()
    {
        await EnsureBytesAsync(1);
        var ret = (sbyte)Peek()[0];
        Advance(1);
        return ret;
    }
    #endregion
}
