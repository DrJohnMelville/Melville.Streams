using System;

namespace Melville.StreamInterfaces;

/// <summary>
/// Represents a stream that knows its current position from the beginning.
/// </summary>
public interface IStreamPosition : IStream
{
    /// <summary>
    /// Current position, in bytes, from the beginning of the stream.
    /// </summary>
    long Position { get; }
}
