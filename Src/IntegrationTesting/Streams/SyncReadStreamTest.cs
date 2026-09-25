using Melville.StreamInterfaces;
using System;
using System.IO;

namespace IntegrationTesting.Streams;

public class SyncReadStreamTest
{

    MockSyncReader innerSut = new();
    ISyncReader Sut => innerSut;
    private readonly byte[] buffer = new byte[8];
    private class MockSyncReader : ISyncReader
    {
        public int MaxSize { get; set; } = int.MaxValue;
        public int ReadCount { get; set; }

        public int Read(Span<byte> buffer)
        {
            buffer.Fill(28);
            ReadCount++;
            return Math.Min(MaxSize,buffer.Length);
        }
    }

    [Test]
    public void ReadAsArray()
    {
        Sut.Read(buffer, 2, 3).Should().Be(3);
        buffer.Should().BeEquivalentTo([0, 0, 28, 28, 28, 0, 0, 0]);
    }

    [Test]
    [Arguments(-1)]
    [Arguments(-10)]
    [Arguments(9)]
    [Arguments(28)]
    public void ReadAtLeastExceptions(int desiredSize)
    {
        Sut.Invoking(i => i.ReadAtLeast(buffer, desiredSize)).Should()
            .Throw<ArgumentException>();
    }

    [Test]
    [Arguments(1, 4, 4)]
    [Arguments(2, 4, 2)]
    [Arguments(3, 4, 2)]
    [Arguments(4, 4, 1)]
    [Arguments(8, 5, 1)]
    public void ReadAtLeast(int maxReadSize, int desiredReads, int totalReads)
    {
        innerSut.MaxSize = maxReadSize;
        Sut.ReadAtLeast(buffer, desiredReads).Should().Be(totalReads * maxReadSize);
        innerSut.ReadCount.Should().Be(totalReads);
    }

    [Test]
    public void ReadOffEndCausesException()
    {
        Sut.ReadAtLeast(buffer, 4);
        innerSut.MaxSize = 0;
        Sut.Invoking(i => i.ReadAtLeast(buffer, 4)).Should()
            .Throw<EndOfStreamException>();
    }

    [Test]
    public void ReadOffEndSucceeds()
    {
        Sut.ReadAtLeast(buffer, 4).Should().Be(8);
        innerSut.MaxSize = 0;
        Sut.ReadAtLeast(buffer, 4, false).Should().Be(0);
    }

    [Test]
    public void ReadExactlyTest()
    {
        innerSut.MaxSize = 3;
        Sut.ReadExact(buffer).Should().Be(8);
    }

}
