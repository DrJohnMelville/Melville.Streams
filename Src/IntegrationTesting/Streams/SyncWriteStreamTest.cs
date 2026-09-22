using Melville.StreamInterfaces;
using System;

namespace IntegrationTesting.Streams;

public class SyncWriteStreamTest
{

    private readonly ISyncWriter sut = new MockSyncWriter();
    private readonly byte[] buffer = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 };
    private class MockSyncWriter : ISyncWriter
    {
        public int Length { get; set; }
        public void Write(ReadOnlySpan<byte> buffer)
        {
            ((ReadOnlySpan<byte>)[2, 3, 4]).SequenceEqual(buffer);
            Length = buffer.Length;
        }
    }

    [Test]
    public void WriteAsArray()
    {
        sut.Write(buffer, 2, 3);
        ((MockSyncWriter)sut).Length.Should().Be(3);
    }

}
