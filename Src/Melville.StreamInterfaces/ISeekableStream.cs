using System.IO;

namespace Melville.StreamInterfaces;

public interface ISeekableStream : IStream
{
    void Seek(long position);
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
