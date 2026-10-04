using Melville.StreamInterfaces.Streams;
using System;
using System.IO;

namespace IntegrationTesting.Streams;

public class StreamToInterfaceCapabilityTesting
{
    private void DoTest(bool read, bool write, bool seek, bool succeed, Func<Stream, object> creator)
    {
        var mock = Stream.Mock();
        mock.CanRead.Returns(read);
        mock.CanWrite.Returns(write);
        mock.CanSeek.Returns(seek);
        if (succeed)
        {
            creator(mock.Object); // should not throw
        }
        else
        {
            mock.Object.Invoking(i => creator(i)).Should().Throw<InvalidOperationException>();
        }
    }

    [Test]
    [Arguments(false, false, false, false)]
    [Arguments(false, true, false, false)]
    [Arguments(true, false, false, true)]
    [Arguments(true, true, false, true)]
    [Arguments(false, false, true, false)]
    [Arguments(false, true, true, false)]
    [Arguments(true, false, true, true)]
    [Arguments(true, true, true, true)]
    public void CreateReader(bool read, bool write, bool seek, bool succeed) =>
        DoTest(read, write, seek, succeed, i => i.AsStreamReader());

    [Test]
    [Arguments(false, false, false, false)]
    [Arguments(false, true, false, true)]
    [Arguments(true, false, false, false)]
    [Arguments(true, true, false, true)]
    [Arguments(false, false, true, false)]
    [Arguments(false, true, true, true)]
    [Arguments(true, false, true, false)]
    [Arguments(true, true, true, true)]
    public void CreateWriter(bool read, bool write, bool seek, bool succeed) =>
        DoTest(read, write, seek, succeed, i => i.AsStreamWriter());

    [Test]
    [Arguments(false, false, false, false)]
    [Arguments(false, true, false, false)]
    [Arguments(true, false, false, false)]
    [Arguments(true, true, false, true)]
    [Arguments(false, false, true, false)]
    [Arguments(false, true, true, false)]
    [Arguments(true, false, true, false)]
    [Arguments(true, true, true, true)]
    public void CreateReaderWriter(bool read, bool write, bool seek, bool succeed) =>
        DoTest(read, write, seek, succeed, i => i.AsStreamReaderWriter());
    [Test]
    [Arguments(false, false, false, false)]
    [Arguments(false, true, false, false)]
    [Arguments(true, false, false, false)]
    [Arguments(true, true, false, false)]
    [Arguments(false, false, true, false)]
    [Arguments(false, true, true, false)]
    [Arguments(true, false, true, true)]
    [Arguments(true, true, true, true)]

    public void CreateSeekableReader(bool read, bool write, bool seek, bool succeed) =>
        DoTest(read, write, seek, succeed, i => i.AsSeekableStreamReader());

    [Test]
    [Arguments(false, false, false, false)]
    [Arguments(false, true, false, false)]
    [Arguments(true, false, false, false)]
    [Arguments(true, true, false, false)]
    [Arguments(false, false, true, false)]
    [Arguments(false, true, true, true)]
    [Arguments(true, false, true, false)]
    [Arguments(true, true, true, true)]
    public void CreateSeekableWriter(bool read, bool write, bool seek, bool succeed) =>
        DoTest(read, write, seek, succeed, i => i.AsSeekableStreamWriter());

    [Test]
    [Arguments(false, false, false, false)]
    [Arguments(false, true, false, false)]
    [Arguments(true, false, false, false)]
    [Arguments(true, true, false, false)]
    [Arguments(false, false, true, false)]
    [Arguments(false, true, true, false)]
    [Arguments(true, false, true, false)]
    [Arguments(true, true, true, true)]
    public void CreateSeekableReaderWriter(bool read, bool write, bool seek, bool succeed) =>
        DoTest(read, write, seek, succeed, i => i.AsSeekableStreamReaderWriter());
}
