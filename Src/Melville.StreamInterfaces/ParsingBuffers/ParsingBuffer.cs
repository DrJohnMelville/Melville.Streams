using Melville.INPC;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces.ParsingBuffers;

public partial class ParsingBuffer(IAsyncReader source, int initialBufferSize = 8 * 1024) : IDisposable
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

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(buffer);
        buffer = [];
    }
    #endregion

    #region Used portions of buffer
    private int firstByte;
    private int nextByteSpace;
    public Span<byte> Peek() => buffer.AsSpan(firstByte, CurrentLength);
    public Memory<byte> PeekMemory() => buffer.AsMemory(firstByte, CurrentLength);
    public int CurrentLength => nextByteSpace - firstByte;
    public long RelativePosition { get; set; }

    public void Advance(int bytes)
    {
        firstByte += bytes;
        if (firstByte > nextByteSpace)
            throw new InvalidOperationException("Claimed more bytes than the buffer actually contains");
        RelativePosition += bytes;
    }
    #endregion

    #region Read additional bytes into the buffer
    public ValueTask<int> TryEnsureBytesAsync(int desiredSize, CancellationToken ct = default) =>
        CurrentLength >= desiredSize ?
            ValueTask.FromResult(0) :
            ReadBytesAsync(desiredSize, ct);

    public async ValueTask<int> EnsureBytesAsync(int desiredSize, CancellationToken ct = default)
    {
        var ret = await TryEnsureBytesAsync(desiredSize).CA();
        if (CurrentLength < desiredSize && !ct.IsCancellationRequested)
            throw new InvalidOperationException("Not enough bytes for EnsureBytesAsync");
        return ret;
    }

    public async ValueTask<int> ReadBytesAsync(int desiredSize = 0, CancellationToken ct = default)
    {
        TryCycleBuffer(desiredSize);
        var bytesNeeded = Math.Max(1, desiredSize - CurrentLength);
        var ret =
            await source.ReadAtLeastAsync(buffer.AsMemory(nextByteSpace), bytesNeeded, ct, false).CA();
        nextByteSpace += ret;
        return ret;
    }
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
    public async ValueTask FillExternalBufferAsync(Memory<byte> externalBuffer, CancellationToken ct = default)
    {
        var read = await TryFillExternalBufferAsync(externalBuffer, ct);
        if (read < externalBuffer.Length && !ct.IsCancellationRequested)
            throw new InvalidOperationException("Not enough bytes tof fill buffer");
    }

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
    public async ValueTask<Memory<byte>> TakeBytes(int bytes)
    {
        await EnsureBytesAsync(bytes);
        var ret = PeekMemory()[..bytes];
        Advance(bytes);
        return ret;
    }

    [MacroItem(16, 2)]
    [MacroItem(32, 4)]
    [MacroItem(64, 8)]
    [MacroItem(128, 16)]
    [MacroCode("""
          public async ValueTask<UInt~0~> GetUInt~0~BigEndianAsync() =>
               BinaryPrimitives.ReadUInt~0~BigEndian((await TakeBytes(~1~)).Span);
           public async ValueTask<UInt~0~> GetUInt~0~LittleEndianAsync() =>
               BinaryPrimitives.ReadUInt~0~LittleEndian((await TakeBytes(~1~)).Span);
           public async ValueTask<Int~0~> GetInt~0~BigEndianAsync() =>
               BinaryPrimitives.ReadInt~0~BigEndian((await TakeBytes(~1~)).Span);
           public async ValueTask<Int~0~> GetInt~0~LittleEndianAsync() =>
               BinaryPrimitives.ReadInt~0~LittleEndian((await TakeBytes(~1~)).Span);
        """)]
    public async ValueTask<byte> GetUInt8(){
        await EnsureBytesAsync(1);
        var ret = Peek()[0];
        Advance(1);
        return ret;
    }
    public async ValueTask<sbyte> GetInt8()
    {
        await EnsureBytesAsync(1);
        var ret = (sbyte)Peek()[0];
        Advance(1);
        return ret;
    }
    #endregion
}
