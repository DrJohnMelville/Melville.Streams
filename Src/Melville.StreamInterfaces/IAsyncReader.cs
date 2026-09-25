using System;
using System.Threading;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces;

public interface IAsyncReader : IStream
{
    ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation = default);
    ValueTask<int> ReadAsync(byte[] buffer, int position, int length, CancellationToken cancellation = default) =>
        ReadAsync(buffer.AsMemory(position, length), cancellation);
}
