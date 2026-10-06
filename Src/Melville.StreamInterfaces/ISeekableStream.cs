using System.IO;

namespace Melville.StreamInterfaces;

/// <summary>
/// Represents a stream that the current position can be repositioned.
/// </summary>
public interface ISeekableStream : IStream
{
    /// <summary>
    /// Set the current position, relative to the beginning of the stream.
    /// </summary>
    /// <param name="position">The position for the next read or write operation.</param>
    void Seek(long position);

    /// <summary>
    /// Seek toa position relative to a given origin.
    /// </summary>
    /// <param name="position">Position of the next read or write, relative to the given origin.</param>
    /// <param name="origin">The desired origin for the seek operation.</param>
    /// <returns>The final position, relative to the beginning of the stream.</returns>
    public long Seek(long position, SeekOrigin origin)
    {
        var innerPos = position + origin switch
        {
            SeekOrigin.Current => this.PositionOrException,
            SeekOrigin.End => this.LengthOrException,
            _ => 0
        };
        Seek(innerPos);
        return innerPos;
    }
}
