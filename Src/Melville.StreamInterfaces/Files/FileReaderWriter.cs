using Melville.IntersectionTypes;
using Melville.StreamInterfaces.Memory;
using Melville.StreamInterfaces.Mixins;
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
public class FileStreamBase: RandomAccessSeekableStreamBase,
    IStreamLength, IDisposable
{
    /// <summary>
    /// Create a FileStreamBase
    /// </summary>
    /// <param name="handle">the desired handle for the stream.</param>
    protected FileStreamBase(SafeFileHandle handle)
    {
        this.handle = handle;
        RequireDisposal.ReportCreation(this);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        RequireDisposal.ReportDisposal(this);
        GC.SuppressFinalize(this);
        handle.Dispose();
    }

    /// <summary>
    /// Finalize the FileStreamBase -- to make sure it is disposed.
    /// </summary>
    ~FileStreamBase() => RequireDisposal.ReportMissedDispose(this);

    /// <summary>
    /// A windows file handle representing the file.
    /// </summary>
    protected internal readonly SafeFileHandle handle;

    /// <inheritdoc/>
    public long Length => RandomAccess.GetLength(handle);
 
    internal readonly struct FileStreamRead (FileStreamBase self): IAsyncReader, ISyncReader
    {
        /// <summary>
        /// Read from the stream into the buffer synchronously.
        /// </summary>
        /// <param name="buffer">The buffer to place bytes into.</param>
        /// <returns>Number of bytes read.</returns>
        public int Read(Span<byte> buffer) => 
            self.BumpPosition(RandomAccess.Read(self.handle, buffer, self.Position));

        /// <summary>
        /// Read bytes from the stream into the buffer asynchronously.
        /// </summary>
        /// <param name="buffer">The buffer to fill with the requested data.</param>
        /// <param name="cancellation">A cancellation token</param>
        /// <returns>The number of bytes read.</returns>
        public async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation = default) =>
          self.BumpPosition
            (await RandomAccess.ReadAsync(self.handle, buffer, self.Position, cancellation));

    }

    internal readonly struct FileStreamWrite(FileStreamBase self)
    {
        /// <summary>
        /// Write data to the stream synchronously.
        /// </summary>
        /// <param name="buffer">The data to write to the stream.</param>
        public void Write(ReadOnlySpan<byte> buffer)
        {
            RandomAccess.Write(self.handle, buffer, self.Position);
            self.BumpPosition(buffer.Length);
        }

        /// <summary>
        /// Write data to a stream asynchronously.
        /// </summary>
        /// <param name="buffer">The data to write to the stream.</param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
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
