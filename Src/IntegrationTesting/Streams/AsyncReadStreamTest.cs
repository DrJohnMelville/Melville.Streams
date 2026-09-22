using Melville.StreamInterfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationTesting.Streams;

public class AsyncReadStreamTest
{
    private class MockAsyncReader : IAsyncReader
    {
        public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation = default)
        {
            buffer.Span.Fill(28);
            return new(buffer.Length);
        }
    }

    private readonly IAsyncReader sut = new MockAsyncReader();
    private readonly byte[] buffer = new byte[8];

    [Test]
    public async Task ReadFromArray()
    {
        (await sut.ReadAsync(buffer, 2, 3)).Should().Be(3);
        buffer.Should().BeEquivalentTo([0, 0, 28, 28, 28, 0, 0, 0]);
    }
}

