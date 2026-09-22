using Melville.StreamInterfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IntegrationTesting.Streams;

public class SeekImplementationTest
{

    private class SeekMock(ISeekableStream inner) : ISeekableStream, IStreamPosition, IStreamLength
    {
        public long Position => ((IStreamPosition)inner).Position;

        public long Length => ((IStreamLength)inner).Length;

        public void Seek(long position) => inner.Seek(position);
    }


    private class SkinnySeekMock(ISeekableStream inner) : ISeekableStream
    {
        public void Seek(long position) => inner.Seek(position);
    }

    [Test]
    [MatrixDataSource]
    public void SeekFromBeginning([Matrix(0, 1, 210)] long position)
    {
        var source = ISeekableStream.Mock();

        ((ISeekableStream)new SeekMock(source.Object)).Seek(position, SeekOrigin.Begin).Should().Be(position);
        source.Seek(position).WasCalled(Times.Once);
    }

    public interface Combined: ISeekableStream, IStreamPosition, IStreamLength
    {

    }
    [Test]
    [MatrixDataSource]
    public void SeekFromCurrentPos([Matrix(0, 1, 210)] long position)
    {
        var source = Combined.Mock();
        source.Position.Returns(1000);
        ((ISeekableStream)new SeekMock(source.Object)).Seek(position, SeekOrigin.Current).Should().Be(position + 1000);
        source.Seek(position + 1000).WasCalled(Times.Once);
    }
    [Test]
    [MatrixDataSource]
    public void SeekFromEnd([Matrix(0, 1, 210)] long position)
    {
        var source = Combined.Mock();
        source.Length.Returns(1000);
        ((ISeekableStream)new SeekMock(source.Object)).Seek(position, SeekOrigin.End).Should().Be(position + 1000);
        source.Seek(position + 1000).WasCalled(Times.Once);
    }

    [Test]
    [Arguments(SeekOrigin.Current, "Can only use SeekOrigin.Current in streams that implement IStreamPosition")]
    [Arguments(SeekOrigin.End, "Can only use SeekOrigin.End in streams that implement IStreamLength")]
    public void NeedIStreamPositionToSeekRelativeToPosition(SeekOrigin origin, string message)
    {
        var source = ISeekableStream.Mock();
        ((ISeekableStream)new SkinnySeekMock(source.Object)).Invoking(i => i.Seek(10, origin))
            .Should().ThrowExactly<InvalidOperationException>()
            .WithMessage(message );
    }
}
