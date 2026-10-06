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
    /// <summary>
    /// A windows file handle representing the file.
    /// </summary>
    protected internal readonly SafeFileHandle handle = handle;

    /// <inheritdoc/>
    public long Length => RandomAccess.GetLength(handle);
    /// <inheritdoc/>
    public void Dispose() => handle.Dispose();

    internal readonly struct FileStreamRead (FileStreamBase self): IAsyncReader, ISyncReader
    {
        public int Read(Span<byte> buffer) => 
            self.BumpPosition(RandomAccess.Read(self.handle, buffer, self.Position));

        public async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation = default) =>
          self.BumpPosition
            (await RandomAccess.ReadAsync(self.handle, buffer, self.Position, cancellation));

    }

    internal readonly struct FileStreamWrite(FileStreamBase self)
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

/// <summary>
/// Represents a file stream that supports only reading.
/// </summary>
/// <param name="handle">A windows file handle representing the file.</param>
public partial class FileReader(SafeFileHandle handle) : 
    FileStreamBase(handle),IMixin<FileStreamRead>
{

}

/// <summary>
/// Represents a file stream that supports only writing.
/// </summary>
/// <param name="handle">A windows file handle representing the file.</param>
public partial class FileWriter(SafeFileHandle handle) : FileStreamBase(handle), IMixin<FileStreamWrite>
{
}

/// <summary>
/// Represents a file stream that supports reading and writing.
/// </summary>
/// <param name="handle">A windows file handle representing the file.</param>
public partial class FileReaderWriter(SafeFileHandle handle): FileStreamBase(handle),
    IMixin<FileStreamRead>, IMixin<FileStreamWrite>
{
}
