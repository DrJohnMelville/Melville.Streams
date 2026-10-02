using Melville.StreamInterfaces;
using Melville.StreamInterfaces.Streams;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationTesting.Streams;

public class InterfaceToStreamAdapterTest
{
    public interface IDisposableStream : IStream, IDisposable, IAsyncDisposable { }
    [Test]
    public void DisposeDisposableInterface()
    {
        var mock = Mock.Of<IDisposableStream>();
        mock.Object.AsStream().Dispose();
        mock.Dispose().WasCalled(Times.Once);
    }
    [Test]
    public void DisposeNonDisposableInterface()
    {
        var mock = Mock.Of<IStream>();
        mock.Object.AsStream().Dispose();
        // should not throw but obviously cannot call a 
    }
    [Test]
    public async Task DisposeDisposableInterfaceAsync()
    {
        var mock = Mock.Of<IDisposableStream>();
        await mock.Object.AsStream().DisposeAsync();
        mock.DisposeAsync().WasCalled(Times.Once);
    }
    [Test]
    public async Task  DisposeNonDisposableInterfaceAsynbc()
    {
        var mock = Mock.Of<IStream>();
        await mock.Object.AsStream().DisposeAsync();
        // should not throw but obviously cannot call a 
    }

    [Test]
    public void ReadSyncFromSync()
    {
        var mock = Mock.Of<ISyncReader>();
        mock.Read(RefStructArg<Span<byte>>.Any).Returns(10);
        var sut = mock.Object.AsStream();
        sut.Read([]).Should().Be(10);

    }
    [Test]
    public async Task ReadAsyncFromSync()
    {
        var mock = Mock.Of<ISyncReader>();
        mock.Read(RefStructArg<Span<byte>>.Any).Returns(10);
        var sut = mock.Object.AsStream();
        (await sut.ReadAsync(new Memory<byte>())).Should().Be(10);

    }
    [Test]
    public void ReadSyncFromAsync()
    {
        var mock = Mock.Of<IAsyncReader>();
        mock.ReadAsync(Arg.Any(), Arg.Any()).Returns(10);
        var sut = mock.Object.AsStream();
        sut.Read(stackalloc byte[20]).Should().Be(10);

    }
    [Test]
    public async Task ReadAsyncFromAync()
    {
        var mock = Mock.Of<IAsyncReader>();
        mock.ReadAsync(Arg.Any(), Arg.Any()).Returns(10);
        var sut = mock.Object.AsStream();
        (await sut.ReadAsync(new Memory<byte>(new byte[20]))).Should().Be(10);

    }

    [Test]
    public void WriteSyncFromSync()
    {
        var mock = Mock.Of<ISyncWriter>();
        var sut = mock.Object.AsStream();
        sut.Write([]);
        mock.Write(RefStructArg<ReadOnlySpan<byte>>.Any).WasCalled(Times.Once);

    }
    [Test]
    public async Task WriteAsyncFromSync()
    {
        var mock = Mock.Of<ISyncWriter>();
        var sut = mock.Object.AsStream();
        await sut.WriteAsync(new Memory<byte>());
        mock.Write(RefStructArg<ReadOnlySpan<byte>>.Any).WasCalled(Times.Once);

    }
    [Test]
    public void WriteSyncFromAsync()
    {
        var mock = Mock.Of<IAsyncWriter>();
        var sut = mock.Object.AsStream();
        sut.Write(stackalloc byte[20]);
        mock.WriteAsync(Arg.Any(), Arg.Any()).WasCalled(Times.Once);

    }
    [Test]
    public async Task WriteAsyncFromAync()
    {
        var mock = Mock.Of<IAsyncWriter>();
        var sut = mock.Object.AsStream();
        await sut.WriteAsync(new Memory<byte>(new byte[20]));
        mock.WriteAsync(Arg.Any(), Arg.Any()).WasCalled(Times.Once);

    }

    [Test]
    public void CanReadTest()
    {
        ISyncReader.Mock().AsStream().CanRead.Should().BeTrue();
        IAsyncReader.Mock().AsStream().CanRead.Should().BeTrue();
        ISyncWriter.Mock().AsStream().CanRead.Should().BeFalse();
    }
    [Test]
    public void CanWriteTest()
    {
        ISyncWriter.Mock().AsStream().CanWrite.Should().BeTrue();
        IAsyncWriter.Mock().AsStream().CanWrite.Should().BeTrue();
        ISyncReader.Mock().AsStream().CanWrite.Should().BeFalse();
    }
    [Test]
    public void CanSeekTest()
    {
        ISeekableStream.Mock().AsStream().CanSeek.Should().BeTrue();
        ISyncReader.Mock().AsStream().CanSeek.Should().BeFalse();
    }

    [Test]
    public void LengthTest()
    {
        var mock = IStreamLength.Mock();
        mock.Length.Returns(32);
        mock.Object.AsStream().Length.Should().Be(32);
    }

    [Test]
    public void ReadPositionTest()
    {
        var mock = IStreamPosition.Mock();
        mock.Position.Returns(15);
        mock.AsStream().Position.Should().Be(15);
    }


}
