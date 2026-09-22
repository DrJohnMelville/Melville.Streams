using Melville.StreamInterfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationTesting.Streams;

public class AsyncWriteStreamTest
{
    private class MockAsyncWriter : IAsyncWriter
    {
        public int Length { get; set; }
        public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellation = default)
        {
            Span<byte> answer = stackalloc byte[] { 2, 3, 4 };
            answer.SequenceEqual(buffer.Span).Should().BeTrue();
            Length = buffer.Length;
            return new();
        }
    }

    private readonly IAsyncWriter sut = new MockAsyncWriter();
    private readonly byte[] buffer = [0, 1, 2, 3, 4, 5, 6, 7];

    [Test]
    public async Task WriteFromArray()
    {
        await sut.WriteAsync(buffer, 2, 3);
        ((MockAsyncWriter)sut).Length.Should().Be(3);
    }
}

