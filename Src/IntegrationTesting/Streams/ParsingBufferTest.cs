
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

    public ParsingBufferTest() => sut = new(source, 16);

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
    }

    [Test]
    public async Task NoMoreBytesAvailable()
    {
        source.Length = 0;
        (await sut.TryEnsureBytesAsync(12)).Should().Be(0);
        await sut.Awaiting(i => i.EnsureBytesAsync(12)).Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task ClaimBytes()
    {
        sut.RelativePosition = 100;
        await sut.EnsureBytesAsync(16);
        sut.Advance(3);
        sut.CurrentLength.Should().Be(13);
        sut.Peek().ToArray().Should().BeEquivalentTo([
            3, 4,5,6,7,8,9,10,11,12,13,14,15]);
        sut.Advance(4);
        sut.RelativePosition.Should().Be(4);
        sut.CurrentLength.Should().Be(9);
        sut.Peek().ToArray().Should().BeEquivalentTo([
            7,8,9,10,11,12,13,14,15]);
    }

    [Test]
    public async Task CycleBuffer()
    {
        await sut.EnsureBytesAsync(16);
        sut.Advance(7);
        await sut.EnsureBytesAsync(16);
        sut.CurrentLength.Should().Be(16);
        sut.Peek().ToArray().Should().BeEquivalentTo(
            [7, 8, 9, 10, 11, 12, 13, 14, 15, 0, 1, 2, 3, 4, 5, 6]);

    }
    [Test]
    public async Task ExpandWhenFull()
    {
        await sut.EnsureBytesAsync(16);
        await sut.ReadBytesAsync();
        sut.CurrentLength.Should().Be(32);
        sut.Peek().ToArray().Should().BeEquivalentTo(
            [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 
             0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15]);

    }
    [Test]
    public async Task ExpandWhenDesiredMore()
    {
        source.Length = 1000;
        await sut.EnsureBytesAsync(150);
        sut.CurrentLength.Should().Be(256);

    }

    [Test]
    public async Task FillExternalBufferFromInternalBuffer()
    {
        source.Length = 8;
        await sut.EnsureBytesAsync(16);
        source.Length = 0;

        var buffer = new byte[4];
        await sut.FillExternalBufferAsync(buffer);
        buffer.Should().BeEquivalentTo([0, 1, 2, 3]);
        await sut.FillExternalBufferAsync(buffer);
        buffer.Should().BeEquivalentTo([4, 5, 6, 7]);
        await sut.FillExternalBufferAsync(buffer);
        buffer.Should().BeEquivalentTo([0, 1, 2, 3]);
        await sut.FillExternalBufferAsync(buffer);
        buffer.Should().BeEquivalentTo([4, 5, 6, 7]);

        await sut.Awaiting(i => i.FillExternalBufferAsync(buffer)).Should().
            ThrowExactlyAsync<InvalidOperationException>();
    }

    [Test]
    public async Task FillExternalBufferFromStream()
    {
        source.Length = 4;
        await sut.EnsureBytesAsync(4);

        var buffer = new byte[6];
        await sut.FillExternalBufferAsync(buffer);
        buffer.Should().BeEquivalentTo([0, 1, 2, 3, 0, 1]);
        sut.CurrentLength.Should().Be(0);
    }

    [Test]
    public async Task FillExternalBufferExclusiveFromStream()
    {
        source.Length = 5;

        var buffer = new byte[6];
        await sut.FillExternalBufferAsync(buffer);
        buffer.Should().BeEquivalentTo([0, 1, 2, 3, 4, 0]);
        sut.CurrentLength.Should().Be(0);
    }

    [Test] public async Task GetUint16BigEndian() => (await sut.GetUInt16BigEndianAsync()).Should().Be(0x0001);
    [Test] public async Task GetUint16LittleEndian() => (await sut.GetUInt16LittleEndianAsync()).Should().Be(0x0100);
    [Test] public async Task GetInt16BigEndian() => (await sut.GetInt16BigEndianAsync()).Should().Be(0x0001);
    [Test] public async Task GetInt16LittleEndian() => (await sut.GetInt16LittleEndianAsync()).Should().Be(0x0100);
}
