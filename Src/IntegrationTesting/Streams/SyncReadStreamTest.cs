using Melville.StreamInterfaces;
using System;

namespace IntegrationTesting.Streams;

public class SyncReadStreamTest
{

    ISyncReader sut = new MockSyncReader();
    private readonly byte[] buffer = new byte[8];
    private class MockSyncReader : ISyncReader
    {
        public int Read(Span<byte> buffer)
        {
            buffer.Fill(28);
            return buffer.Length;
        }
    }

    [Test]
    public void ReadAsArray()
    {
        sut.Read(buffer, 2, 3).Should().Be(3);
        buffer.Should().BeEquivalentTo([0, 0, 28, 28, 28, 0, 0, 0]);
    }

}
