using Melville.StreamInterfaces.Files;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IntegrationTesting.Streams;

public class FileReaderWriterTest
{
    [Test]
    [MatrixDataSource]
    public async Task SyncWriteTest(
        [Matrix(true, false)] bool readOp,
        [Matrix(true, false)] bool writeOp
        )
    {
        var buffer = Enumerable.Range(0, 2500).Select(i => (byte)i).ToArray();
        var buffer2 = new byte[2500];
        using var pipe = FileIStreamFactory.AnonymousPipe(true);
        if (writeOp)
            pipe.Writer.Write(buffer);
        else
            await pipe.Writer.WriteAsync(buffer);

        if (readOp)
            pipe.Reader.Read(buffer2);
        else
            await pipe.Reader.ReadAsync(buffer2);

        buffer.Should().BeEquivalentTo(buffer2);

    }
}