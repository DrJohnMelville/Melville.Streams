using System;

namespace Melville.StreamInterfaces;

/// <summary>
/// Represents a stream that can be read synchronously.
/// </summary>
public interface ISyncReader : IStream
{
    /// <summary>
    /// Read bytes from the stream into the given buffer.
    /// </summary>
    /// <param name="buffer">Buffer to read bytes into</param>
    /// <returns>The number of bytes read</returns>
    int Read(Span<byte> buffer);

    /// <summary>
    /// Read bytes from the stream into a given buffer.
    /// </summary>
    /// <param name="buffer">The buffer to read bytes into.</param>
    /// <param name="position">Starting position in the buffer.</param>
    /// <param name="length">Maximum number of bytes to read.</param>
    /// <returns>The number of bytes read</returns>
    int Read(byte[] buffer, int position, int length) => Read(buffer.AsSpan(position, length));
}
