
using Melville.StreamInterfaces;
using Melville.StreamInterfaces.ParsingBuffers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationTesting.Streams;

public class FakeRead : IAsyncReader
{
    public int Length { get; set; } = 16;
    public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation = default)
    {
        var len = Math.Min(Length, buffer.Length);
        var sp = buffer.Span[..len];
        for (int i = 0; i < sp.Length; i++)
        {
            sp[i] = (byte)i;
        }
        return ValueTask.FromResult(len);
    }
}

public class ParsingBufferTest : IDisposable
{
    private readonly FakeRead source = new();
    private readonly ParsingBuffer sut;

    public void Dispose() => sut.Dispose();

    public ParsingBufferTest() => sut = new(source);

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(4)]
    public async Task ReadFromSource(int reads)
    {
        source.Length = 4;
        sut.Peek().Length.Should().Be(0);
        for (int i = 0; i < reads; i++)
        {
            (await sut.ReadBytesAsync()).Should().Be(4);
            sut.Peek().Length.Should().Be((i+1) * 4);
            sut.Peek()[^4..].ToArray().Should().BeEquivalentTo([0, 1, 2, 3]);
        }
    }

    [Test]
    [Arguments(1)]
    [Arguments(4)]
    [Arguments(5)]
    public async Task ReadAtLeast(int readLen)
    {
        source.Length = readLen;
        (await sut.ReadBytesAsync(16)).Should().Be(16);
        sut.Peek().ToArray().Should().BeEquivalentTo(
            Enumerable.Range(0, 16).Select(i=>(byte)i%readLen));
    }

    [Test]
    public async Task TryEnsureBytesAsync()
    {
        source.Length = 4;
        await sut.EnsureBytesAsync(6);
        sut.CurrentLength.Should().Be(8);
        await sut.EnsureBytesAsync(6);
        (await sut.EnsureBytesAsync(6)).Should().Be(0);
        sut.CurrentLength.Should().Be(8);
        source.Length = 0;
        (await sut.TryEnsureBytesAsync(12)).Should().Be(0);
        await sut.Awaiting(i => i.EnsureBytesAsync(12)).Should()
            .ThrowAsync<InvalidOperationException>();
    }
}
