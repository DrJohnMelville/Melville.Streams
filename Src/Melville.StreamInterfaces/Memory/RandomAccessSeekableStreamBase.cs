using System;

namespace Melville.StreamInterfaces.Memory;

public class RandomAccessSeekableStreamBase : ISeekableStream, IStreamPosition
{
    public long Position { get; private set; }

    public void Seek(long position) => Position = position;

    protected int BumpPosition(int delta)
    {
        Position += delta;
        return delta;
    }

}
