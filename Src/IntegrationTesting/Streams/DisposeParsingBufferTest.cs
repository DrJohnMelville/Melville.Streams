using Melville.StreamInterfaces.ParsingBuffers;
using System;
using System.Linq;
using static TUnit.Mocks.Times;

namespace IntegrationTesting.Streams;

public class DisposeParsingBufferTest
{
    private Mock<IDisposable> d1 = Mock.Of<IDisposable>();
    private Mock<IDisposable> d2 = Mock.Of<IDisposable>();

    [Test]
    public void CanDisposeOne()
    {
        var sut = new ParsingBuffer(new FakeRead()).WithDisposedDependency(d1);
        d1.Dispose().WasCalled(Never);
        sut.Dispose();
        d1.Dispose().WasCalled(Once);

    }
    [Test]
    public void CanDisposeTwo()
    {
        var sut = new ParsingBuffer(new FakeRead())
            .WithDisposedDependency(d1)
            .WithDisposedDependency(d2);
        d1.Dispose().WasCalled(Never);
        d2.Dispose().WasCalled(Never);
        sut.Dispose();
        d1.Dispose().WasCalled(Once);
        d2.Dispose().WasCalled(Once);

    }
}
