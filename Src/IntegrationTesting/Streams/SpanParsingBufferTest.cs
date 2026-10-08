using Melville.StreamInterfaces.ParsingBuffers;
using System;

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
}