using System;
using System.Threading;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces;

/// <summary>
/// Represents a stream that can be read asynchronously.
/// </summary>
public interface IAsyncReader : IStream
{
    /// <summary>
    /// Asynchronously read a stream into the given buffer.
    /// </summary>
    /// <param name="buffer">The buffer to fill from the stream.</param>
    /// <param name="cancellation">A cancellation token</param>
    /// <returns>The number of bytes read.</returns>
    ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation = default);

    /// <summary>
    /// Asynchronously read a stream into a given buffer.
    /// </summary>
    /// <param name="buffer">The buffer to fill from the stream.</param>
    /// <param name="position">The starting position to fill from</param>
    /// <param name="length">Maximum number of bytes to read.</param>
    /// <param name="cancellation">A cancellation token.</param>
    /// <returns>The number of bytes read.</returns>
    ValueTask<int> ReadAsync(byte[] buffer, int position, int length, CancellationToken cancellation = default) =>
        ReadAsync(buffer.AsMemory(position, length), cancellation);
}
