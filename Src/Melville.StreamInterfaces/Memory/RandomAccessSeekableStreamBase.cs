namespace Melville.StreamInterfaces.Memory;


/// <summary>
/// This is an implementation class that keeps track of a current position and implements seeking.
/// This is useful for streams based on a random access backing store.
/// </summary>
public class RandomAccessSeekableStreamBase : ISeekableStream, IStreamPosition
{
    ///<inheritdoc/>
    public long Position { get; private set; }

    ///<inheritdoc/>
    public void Seek(long position) => Position = position;

    /// <summary>
    /// Increment the current position, and then returns that delta.
    /// </summary>
    /// <param name="delta">The amount by which to increment the current position.</param>
    /// <returns>The delta argument.</returns>
    protected int BumpPosition(int delta)
    {
        Position += delta;
        return delta;
    }

}
