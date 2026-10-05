using Microsoft.Win32.SafeHandles;
using System;
using System.IO;

namespace Melville.StreamInterfaces.Files;


/// <summary>
/// This factory creates file system streams using a fluent interface.
/// </summary>
public static class FileIStreamFactory
{
    /// <summary>
    /// This factory intermediate is used to create file streams with various reading and 
    /// writing capabilities.  All file streams are seekable, with position and length.
    /// </summary>
    /// <param name="creator"></param>
    public readonly struct FileCreateStub (Func<FileAccess, SafeFileHandle> creator)
    {
        /// <summary>
        /// Create a reader stream that supports synchronous and asynchronous reading.
        /// </summary>
        public FileReader Reader() => new(creator(FileAccess.Read));
        /// <summary>
        /// Create a writer stream that supports synchronous and asynchronous writer.
        /// </summary>
        public FileWriter Writer() => new(creator(FileAccess.Write));
        /// <summary>
        /// Create a reader / writer stream that supports synchronous and asynchronous 
        /// reading or writing.
        /// </summary>
        public FileReaderWriter ReaderWriter() => new(creator(FileAccess.ReadWrite));
    }

    /// <summary>
    /// Create a file stream with the given information.
    /// </summary>
    /// <param name="path">Path of the file to open.</param>
    /// <param name="mode">FileMode with which to open the file.</param>
    /// <param name="share">File sharing options</param>
    /// <param name="options">Other file options</param>
    /// <param name="preallocationSize">When creating files, hint to the operating system
    /// about the expected size of the file.</param>
    /// <returns></returns>
    public static FileCreateStub 
        File(string path, 
             FileMode mode = FileMode.Open, 
             FileShare share = FileShare.None, 
             FileOptions options = FileOptions.Asynchronous,
             long preallocationSize = 0) =>new(access =>
    {
        VerifyCompatibleFileMode(access, mode);
        return System.IO.File.OpenHandle(path, mode, access, share, options, preallocationSize);
    });

    private static void VerifyCompatibleFileMode(FileAccess fileAccess, FileMode fileMode)
    {
        switch (fileMode, fileAccess)
        {
            case (not FileMode.Open, FileAccess.Read):
                throw new ArgumentException(
                    $"""Opening files with the mode "{fileMode}" mode requires write access.""");
            case (FileMode.Truncate or FileMode.Append, not FileAccess.Write):
                throw new ArgumentException($"""Files opened in "{fileMode}" mode do not support reading.""");
        }
    }

    /// <summary>
    /// Create an anonymous pipe with associated reader and writer.
    /// </summary>
    /// <param name="allowAsync">Allow async operations</param>
    /// <returns>A structure containing the reader and writer for the anonymous pipe.</returns>
    public static AnonymousPipePair AnonymousPipe(bool allowAsync = true) => new AnonymousPipePair(allowAsync);

    /// <summary>
    /// This structure represents a reader / writer pair for an anonymous pipe.
    /// </summary>
    public readonly struct AnonymousPipePair: IDisposable
    {
        /// <summary>
        /// The reader end of the pipe.
        /// </summary>
        public FileReader Reader { get; }

        /// <summary>
        /// The writer end of the pipe.
        /// </summary>
        public FileWriter Writer { get; }

        internal AnonymousPipePair(bool allowAsync)
        {
            SafeFileHandle.CreateAnonymousPipe(out var readHandle, out var writeHandle, allowAsync);
            Reader = new FileReader(readHandle);
            Writer = new FileWriter(writeHandle);
        }
        
        /// <inheritdoc/>
        public void Dispose()
        {
            Writer.Dispose();
            Reader.Dispose();
        }
    }
}
