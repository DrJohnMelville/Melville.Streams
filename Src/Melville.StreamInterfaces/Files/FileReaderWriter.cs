using Melville.IntersectionTypes;
using Melville.StreamInterfaces.Memory;
using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static Melville.StreamInterfaces.Files.FileStreamBase;

namespace Melville.StreamInterfaces.Files;


/// <summary>
/// This is the base of the file stream classes, and implements seek, length, and position
/// </summary>
/// <param name="handle">Windows file handle corresponding to the file.</param>
public class FileStreamBase(SafeFileHandle handle): RandomAccessSeekableStreamBase,
    IStreamLength, IDisposable
{
    protected internal readonly SafeFileHandle handle = handle;

    public long Length => RandomAccess.GetLength(handle);
    public void Dispose() => handle.Dispose();

    public readonly struct FileStreamRead (FileStreamBase self): IAsyncReader, ISyncReader
    {
        public int Read(Span<byte> buffer) => 
            self.BumpPosition(RandomAccess.Read(self.handle, buffer, self.Position));

        public async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation = default) =>
          self.BumpPosition
            (await RandomAccess.ReadAsync(self.handle, buffer, self.Position, cancellation));

    }

    public readonly struct FileStreamWrite(FileStreamBase self)
    {
        public void Write(ReadOnlySpan<byte> buffer)
        {
            RandomAccess.Write(self.handle, buffer, self.Position);
            self.BumpPosition(buffer.Length);
        }

        public async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellation = default)
        {
            await RandomAccess.WriteAsync(self.handle, buffer, self.Position);
            self.BumpPosition(buffer.Length);
        }
    }
}

public partial class FileReader(SafeFileHandle handle) : 
    FileStreamBase(handle),IMixin<FileStreamRead>
{

}

public partial class FileWriter(SafeFileHandle handle) : FileStreamBase(handle), IMixin<FileStreamWrite>
{
}

public partial class FileReaderWriter(SafeFileHandle handle): FileStreamBase(handle),
    IMixin<FileStreamRead>, IMixin<FileStreamWrite>
{
}
