using System;

namespace Melville.StreamInterfaces;

/// <summary>
/// Supports synchronous stream writing
/// </summary>
public interface ISyncWriter : IStream
{
    /// <summary>
    /// Write a buffer to a stream synchronously.
    /// </summary>
    /// <param name="buffer">The data to write to the stream.</param>
    void Write(ReadOnlySpan<byte> buffer);
    /// <summary>
    /// Write a portion of a buffer to a stream synchronously.
    /// </summary>
    /// <param name="source">The data to write to the stream.</param>
    /// <param name="position">The starting position in the buffer.</param>
    /// <param name="length">The length of the data to write.</param>
    void Write(byte[] source, int position, int length) => Write(source.AsSpan(position, length));
}
