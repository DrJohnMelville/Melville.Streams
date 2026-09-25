using System;
using System.Buffers;
using System.IO;

namespace Melville.StreamInterfaces;

public interface ISyncReader : IStream
{
    int Read(Span<byte> buffer);
    int Read(byte[] buffer, int position, int length) => Read(buffer.AsSpan(position, length));
}
