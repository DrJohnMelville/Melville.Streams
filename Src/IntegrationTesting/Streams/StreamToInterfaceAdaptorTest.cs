using Melville.StreamInterfaces;
using Melville.StreamInterfaces.Streams;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationTesting.Streams;

public class StreamToInterfaceAdaptorTest 
{
    private readonly Mock<Stream> inner = Stream.Mock();
    private readonly SeekableStreamReaderWriter sut;
    private readonly byte[] buffer = new byte[10];

    public StreamToInterfaceAdaptorTest()
    {
        inner.CanRead.Returns(true);
        inner.CanWrite.Returns(true);
        inner.CanSeek.Returns(true);
        inner.CanWrite.Returns(true);
        sut = inner.Object.AsSeekableStreamReaderWriter();
    }

    [Test]
    public void SyncReadTest()
    {
        inner.Read(RefStructArg<Span<byte>>.Any).Returns(123);
        sut.Read(buffer.AsSpan()).Should().Be(123);
        inner.Read(RefStructArg<Span<byte>>.Any).WasCalled(Times.Once);
    }

    [Test]
    public void SyncReadFromBuffer()
    {
        inner.Read(Any(), 2, 3).Returns(4);
        sut.Read(buffer, 2, 3).Should().Be(4);
        inner.Read(Any(), 2, 3).WasCalled(Times.Once);
    }

    [Test]
    public async Task AsyncReadTest()
    {
        var mem = buffer.AsMemory();
        inner.ReadAsync(mem, CancellationToken.None).Returns(123);
        (await sut.ReadAsync(mem)).Should().Be(123);
        inner.ReadAsync(mem, CancellationToken.None).WasCalled(Times.Once);
    }
    [Test]
    public async Task AsyncReadFromBuffer()
    {
        inner.ReadAsync(Any(), 2, 3, Any()).Returns(4);
        (await sut.ReadAsync(buffer, 2, 3)).Should().Be(4);
        inner.ReadAsync(Any(), 2, 3, Any()).WasCalled(Times.Once);
    }
    [Test]
    public void SyncWriteTest()
    {
        sut.Write(buffer.AsSpan());
        inner.Write(RefStructArg<ReadOnlySpan<byte>>.Any).WasCalled(Times.Once);
    }

    [Test]
    public void SyncWriteFromBuffer()
    {
        sut.Write(buffer, 2, 3);
        inner.Write(buffer, 2, 3).WasCalled(Times.Once);
    }

    [Test]
    public async Task AsyncWriteTest()
    {
        var mem = buffer.AsMemory();
        await sut.WriteAsync(mem);
        inner.WriteAsync(Any(), CancellationToken.None).WasCalled(Times.Once);
    }
    [Test]
    public async Task AsyncWriteFromBuffer()
    {
        await sut.WriteAsync(buffer, 2, 3);
        inner.WriteAsync(buffer, 2, 3, Any()).WasCalled(Times.Once);
    }

    [Test]
    [MatrixDataSource]
    public void SeekSingle([Matrix(1, 2)] long position)
    {
        inner.Seek(position, SeekOrigin.Begin).Returns(10);
        sut.Seek(position);
        inner.Seek(position, SeekOrigin.Begin).WasCalled(Times.Once);
    }
    [Test]
    [MatrixDataSource]
    public void SeekForwarding([Matrix(1, 2)] long position,
                               [Matrix(SeekOrigin.Begin, SeekOrigin.End)] SeekOrigin origin)
    {
        inner.Seek(position, origin).Returns(10);
        sut.Seek(position, origin).Should().Be(10);

    }

    [Test]
    public void PositionForwarding()
    {
        inner.Position.Returns(72875);
        sut.Position.Should().Be(72875);
    }

    [Test]
    public void LengthForwarding()
    {
        inner.Length.Returns(72875);
        sut.Length.Should().Be(72875);
    }
}
