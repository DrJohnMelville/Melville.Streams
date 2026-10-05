using System;

namespace Melville.StreamInterfaces;

/// <summary>
/// Represents a stream that knows its total length.
/// </summary>
public interface IStreamLength : IStream
{
    /// <summary>
    /// The total length of the stream in bytes.
    /// </summary>
    long Length { get; }
}
