using System;
using System.Threading;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces;

/// <summary>
/// Supports asynchronous stream writing.
/// </summary>
public interface IAsyncWriter : IStream
{
    /// <summary>
    /// Write a buffer to the stream asynchronously.
    /// </summary>
    /// <param name="buffer">The data to write</param>
    /// <param name="cancellation">A cancellation token</param>
    ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellation = default);
    /// <summary>
    /// Writes a buffer to the stream asynchronously.
    /// </summary>
    /// <param name="buffer">The data to write.</param>
    /// <param name="position">The starting position.</param>
    /// <param name="length">Length of the data to write.</param>
    /// <param name="cancellation">CancellationToken for the operation</param>
    /// <returns></returns>
    ValueTask WriteAsync(byte[] buffer, int position, int length, CancellationToken cancellation = default) =>
        WriteAsync(buffer.AsMemory(position, length), cancellation);
}
