using Melville.StreamInterfaces.Memory;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces.Files;


public class FileStreamBase(SafeFileHandle handle): RandomAccessSeekableStreamBase,
    IStreamLength, IDisposable
{
    protected readonly SafeFileHandle handle = handle;
    public long Length => RandomAccess.GetLength(handle);
    public void Dispose() => handle.Dispose();

}

public class FileReader(SafeFileHandle handle) : FileStreamBase(handle),
    IAsyncReader, ISyncReader
{

    public int Read(Span<byte> buffer) => BumpPosition(RandomAccess.Read(handle, buffer, Position));
    
    public async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation = default) =>
        BumpPosition(await RandomAccess.ReadAsync(handle, buffer, Position, cancellation));

}

public class FileWriter(SafeFileHandle handle) : FileStreamBase(handle),
    ISyncWriter, IAsyncWriter
{
    public void Write(ReadOnlySpan<byte> buffer)
    {
        RandomAccess.Write(handle, buffer, Position);
        BumpPosition(buffer.Length);
    }

    public async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellation = default)
    {
        await RandomAccess.WriteAsync(handle, buffer, Position);
        BumpPosition(buffer.Length);
    }
}

public class FileReaderWriter(SafeFileHandle handle): FileWriter(handle), ISyncReader, IAsyncReader
{
    public int Read(Span<byte> buffer) => BumpPosition(RandomAccess.Read(handle, buffer, Position));

    public async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation = default) =>
        BumpPosition(await RandomAccess.ReadAsync(handle, buffer, Position, cancellation));

}
