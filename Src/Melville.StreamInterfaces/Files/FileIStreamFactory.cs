using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.IO.Pipes;

namespace Melville.StreamInterfaces.Files;


public static class FileIStreamFactory
{
    public readonly struct FileCreateStub (Func<FileAccess, SafeFileHandle> creator)
    {
        public FileReader Reader() => new(creator(FileAccess.Read));
        public FileWriter Writer() => new(creator(FileAccess.Write));
        public FileReaderWriter ReaderWriter() => new(creator(FileAccess.ReadWrite));
    }

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

    public static AnonymousPipePair AnonymousPipe(bool allowAsync) => new AnonymousPipePair(allowAsync);

    public readonly struct AnonymousPipePair: IDisposable
    {
        public FileReader Reader { get; }
        public FileWriter Writer { get; }

        public AnonymousPipePair(bool allowAsync)
        {
            SafeFileHandle.CreateAnonymousPipe(out var readHandle, out var writeHandle, allowAsync);
            Reader = new FileReader(readHandle);
            Writer = new FileWriter(writeHandle);
        }

        public void Dispose()
        {
            Writer.Dispose();
            Reader.Dispose();
        }
    }
}
