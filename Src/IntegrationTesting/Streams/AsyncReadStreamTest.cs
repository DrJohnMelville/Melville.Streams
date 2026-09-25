using Melville.StreamInterfaces;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationTesting.Streams;

public class AsyncReadStreamTest
{
    private class MockAsyncReader : IAsyncReader
    {
        public int MaxSize { get; set; } = int.MaxValue;
        public int ReadCount { get; set; }

        public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation = default)
        {
            buffer.Span.Fill(28);
            ReadCount++;
            return new(Math.Min(MaxSize, buffer.Length));
        }
    }

    private readonly MockAsyncReader innerSut = new MockAsyncReader();
    private  IAsyncReader Sut => innerSut;
    private readonly byte[] buffer = new byte[8];

    [Test]
    public async Task ReadFromArray()
    {
        (await Sut.ReadAsync(buffer, 2, 3)).Should().Be(3);
        buffer.Should().BeEquivalentTo([0, 0, 28, 28, 28, 0, 0, 0]);
    }

    [Test]
    [Arguments(-1)]
    [Arguments(-10)]
    [Arguments(9)]
    [Arguments(28)]
    public Task ReadAtLeastExceptions(int desiredSize)
    {
        return Sut.Awaiting(i => i.ReadAtLeastAsync(buffer, desiredSize)).Should()
            .ThrowAsync<ArgumentException>();
    }

    [Test]
    [Arguments(1, 4, 4)]
    [Arguments(2, 4, 2)]
    [Arguments(3, 4, 2)]
    [Arguments(4, 4, 1)]
    [Arguments(8, 5, 1)]
    public async Task ReadAtLeastAsync(int maxReadSize, int desiredReads, int totalReads)
    {
        innerSut.MaxSize = maxReadSize;
        (await Sut.ReadAtLeastAsync(buffer, desiredReads)).Should().Be(totalReads * maxReadSize);
        innerSut.ReadCount.Should().Be(totalReads);
    }

    [Test]
    public async Task ReadOffEndCausesException()
    {
        await Sut.ReadAtLeastAsync(buffer, 4);
        innerSut.MaxSize = 0;
        await Sut.Awaiting(i => i.ReadAtLeastAsync(buffer, 4)).Should()
            .ThrowAsync<EndOfStreamException>();
    }

    [Test]
    public async Task ReadOffEndSucceeds()
    {
        (await Sut.ReadAtLeastAsync(buffer, 4)).Should().Be(8);
        innerSut.MaxSize = 0;
        (await Sut.ReadAtLeastAsync(buffer, 4, CancellationToken.None, false)).Should().Be(0);
    }

    [Test]
    public async Task ReadExactlyAsyncTest()
    {
        innerSut.MaxSize = 3;
        (await Sut.ReadExactAsync(buffer)).Should().Be(8);
    }

}

