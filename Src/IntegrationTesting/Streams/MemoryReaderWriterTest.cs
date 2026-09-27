using Melville.IntersectionTypes;
using Melville.StreamInterfaces;
using Melville.StreamInterfaces.Memory;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationTesting.Streams;

[IntersectionType]
public readonly partial struct ReaderTestItem:
    ISyncReader, IAsyncReader, ISeekableStream, IStreamLength, IStreamPosition
{
}


public abstract class MemoryReaderTestsBase
{
    protected readonly byte[] buffer = [21, 22, 23, 24];
    protected abstract ReaderTestItem Reader { get; }
    
    [Test]
    public void ReadSync()
    {
        Reader.Read(buffer).Should().Be(4);
        buffer.Should().BeEquivalentTo([0, 1, 2, 3]);
        Reader.Read(buffer).Should().Be(4);
        buffer.Should().BeEquivalentTo([4, 5, 6, 7]);
        Reader.Position.Should().Be(8);
    }
    [Test]
    public async Task ReadAsync()
    {
        (await Reader.ReadAsync(buffer)).Should().Be(4);
        buffer.Should().BeEquivalentTo([0, 1, 2, 3]);
        (await Reader.ReadAsync(buffer)).Should().Be(4);
        buffer.Should().BeEquivalentTo([4, 5, 6, 7]);
        Reader.Position.Should().Be(8);
    }

    [Test]
    public void SeekAndReadFromEnd()
    {
        Reader.Seek(14);
        Reader.Position.Should().Be(14);
        Reader.Read(buffer).Should().Be(2);
        buffer.Should().BeEquivalentTo([14, 15, 23, 24]);
    }
}

[InheritsTests]
public class MemoryReaderTest: MemoryReaderTestsBase
{
    private readonly MemoryReader sut = new(
        (byte[])[0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15]);
    protected override ReaderTestItem Reader => sut;
#warning this does not catch the missing componet interface

}

[InheritsTests]
public class MemoryReaderWriterTest: MemoryReaderTestsBase
{

    private readonly MemoryReaderWriter sut = new(
        (byte[])[0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15]);
    protected override ReaderTestItem Reader => sut;


    [Test]
    public void WriteSync()
    {
        sut.Write(buffer);
        sut.Write(buffer);
        sut.AsSpan().ToArray().Should().BeEquivalentTo([21, 22, 23, 24, 21, 22, 23, 24, 8, 9, 10, 11, 12, 13, 14, 15]);
    }
    [Test]
    public void WriteSyncOffEnd()
    {
        sut.Seek(14);
        sut.Write(buffer);
        sut.Length.Should().Be(18);
        sut.AsSpan().Slice(14,4).ToArray().Should()
            .BeEquivalentTo([21, 22, 23, 24]);
    }
    [Test]
    public async Task DoNotWriteIfCanceled ()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        sut.Seek(14);
        await sut.WriteAsync(buffer, cts.Token);
        sut.Position.Should().Be(14);
    }

    [Test]
    public async Task WriteAsync()
    {
        await sut.WriteAsync(buffer);
        await sut.WriteAsync(buffer);
        sut.AsSpan().ToArray().Should().BeEquivalentTo([21, 22, 23, 24, 21, 22, 23, 24, 8, 9, 10, 11, 12, 13, 14, 15]);
    }
}
