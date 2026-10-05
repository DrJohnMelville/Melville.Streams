using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces.ParsingBuffers;

public class ParsingBuffer(IAsyncReader source): IDisposable
{
    private byte[] buffer = ArrayPool<byte>.Shared.Rent(16);
    private int firstByte;
    private int nextByteSpace;

    private int ReadSpaceAvailable => buffer.Length - nextByteSpace;

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(buffer);
        buffer = [];
    }

    public Span<byte> Peek() => buffer.AsSpan(firstByte, CurrentLength);
    public int CurrentLength => nextByteSpace - firstByte;

    public async ValueTask<int> EnsureBytesAsync(int desiredSize, CancellationToken ct = default)
    {
#warning need to cycle to front of buffer.  Need to handle too small buffer.
        var ret = await TryEnsureBytesAsync(desiredSize).CA();
        if (CurrentLength < desiredSize && !ct.IsCancellationRequested)
            throw new InvalidOperationException("Not enough bytes for EnsureBytesAsync");
        return ret;
    }

    public ValueTask<int> TryEnsureBytesAsync(int desiredSize, CancellationToken ct = default) =>
        CurrentLength >= desiredSize ? 
            ValueTask.FromResult(0) :
            ReadBytesAsync(desiredSize, ct); 

    public async ValueTask<int> ReadBytesAsync(int desiredSize = 0, CancellationToken ct = default )
    {
        var bytesNeeded = Math.Max(1, desiredSize - CurrentLength);
        if (bytesNeeded > ReadSpaceAvailable)
            throw new NotImplementedException("Ran out of buffer");
        var ret = 
            await source.ReadAtLeastAsync(buffer.AsMemory(nextByteSpace), bytesNeeded, ct).CA();
        nextByteSpace += ret;
        return ret;
    }
}
