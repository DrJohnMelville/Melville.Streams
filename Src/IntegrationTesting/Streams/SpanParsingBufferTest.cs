using Melville.StreamInterfaces.ParsingBuffers;
using System;
using System.Buffers;

namespace IntegrationTesting.Streams;

public class SpanParsingBufferTest
{
    [Test]
    public void ClaimBytes()
    {
        var sut = new SpanParsingBuffer([1, 2, 3, 4, 5, 6]);
        sut.Peek().ToArray().Should().BeEquivalentTo([1, 2, 3, 4, 5, 6,]);
        sut.Advance(2);
        sut.Peek().ToArray().Should().BeEquivalentTo([3, 4, 5, 6,]);
    }

    [Test]
    public void ParseInt16s()
    {
        var sut = new SpanParsingBuffer([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);
        sut.GetUInt16BigEndian().Should().Be(0x0102);
        sut.GetUInt16LittleEndian().Should().Be(0x0403);
        sut.GetInt16BigEndian().Should().Be(0x0506);
        sut.GetInt16LittleEndian().Should().Be(0x0807);
        sut.GetUInt8().Should().Be(9);
        sut.GetInt8().Should().Be(10);
    }
    [Test]
    public void AdvanceTo()
    {
        var sut = new SpanParsingBuffer([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);
        sut.RelativePosition = 100;
        sut.AdvanceToRelativePosition(104);
        sut.GetUInt16BigEndian().Should().Be(0x0506);

        try
        {
            sut.AdvanceToRelativePosition(105);
        }
        catch (InvalidOperationException)
        {
            return;
        }

        false.Should().BeTrue("The exception should have thrown");
    }

    [Test]
    public void TryPeekAndGetUUnt8()
    {
        var sut = new SpanParsingBuffer([5]);
        sut.TryPeekUInt8(out var num).Should().BeTrue();
        num.Should().Be(5);
        num = 10;
        sut.TryGetUInt8(out num).Should().BeTrue();
        num.Should().Be(5);
        num = 10;
        sut.TryGetUInt8(out num).Should().BeFalse();
        sut.TryPeekUInt8(out num).Should().BeFalse();
    }

    private static readonly SearchValues<byte> SpaceFinder = SearchValues.Create((byte)32);
    private static readonly SearchValues<byte> OneFive = SearchValues.Create([1,2,3,4,5]);

    [Test]
    public void TryGetNextTest()
    {
        var sut = new SpanParsingBuffer([1, 2, 3, 4, 5, 32, 6, 7, 8, 9]);
        sut.TryGetNext(SpaceFinder, out var value).Should().BeTrue();
        value.Should().Be(32);
        sut.RelativePosition.Should().Be(6);
    }
    [Test]
    public void TryGetNextTest2()
    {
        var sut = new SpanParsingBuffer([1, 2, 3, 4, 5, 6, 7, 8, 9]);
        sut.TryGetNext(SpaceFinder, out var value).Should().BeFalse();
        value.Should().Be(0);
        sut.RelativePosition.Should().Be(0);
    }
    [Test]
    public void TryGetNextTes3t()
    {
        var sut = new SpanParsingBuffer([1, 2, 3, 4, 5, 32, 6, 7, 8, 9]);
        sut.TryGetNextExcept(OneFive, out var value).Should().BeTrue();
        value.Should().Be(32);
        sut.RelativePosition.Should().Be(6);
    }
    [Test]
    public void TryGetNextTest4()
    {
        var sut = new SpanParsingBuffer([1, 2, 3, 4, 5, 2, 3, 4, 5]);
        sut.TryGetNextExcept(OneFive, out var value).Should().BeFalse();
        value.Should().Be(0);
        sut.RelativePosition.Should().Be(0);
    }
}