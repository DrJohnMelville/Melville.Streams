using Melville.IntersectionTypes;

namespace Test.IntegrationTests;

public interface IA
{
    public void MA();
}

public interface IB
{
    public int Plus1(int i);
}

[IntersectionType(typeof(IA), typeof(IB))]
public readonly partial struct Intersection { }

public partial class IntersectionIntegration
{

    private readonly Mock<IA> source = Mock.Of<IA, IB>();

    [Test]
    public void ForwardingMethods()
    {
        source.Plus1(1).Returns(2);
        if (Intersection.TryCreateFrom(source.Object, out var sut))
        {
            sut.MA();
            sut.Plus1(1).Should().Be(2);
            sut.MA();
        }
        source.MA().WasCalled(Times.Exactly(2));
        source.Plus1(1).WasCalled(Times.Once);
    }
    [Test]
    public void AsInterface()
    {
        source.Plus1(1).Returns(2);
        if (Intersection.TryCreateFrom(source.Object, out var sut))
        {
            sut.AsIA().MA();
            sut.AsIB().Plus1(1).Should().Be(2);
            sut.AsIA().MA();
        }
        source.MA().WasCalled(Times.Exactly(2));
        source.Plus1(1).WasCalled(Times.Once);
    }
    [Test]
    public void CastToInterface()
    {
        source.Plus1(1).Returns(2);
        if (Intersection.TryCreateFrom(source.Object, out var sut))
        {
            ((IA)sut).MA();
            ((IB)sut).Plus1(1).Should().Be(2);
            ((IA)sut).MA();
        }
        source.MA().WasCalled(Times.Exactly(2));
        source.Plus1(1).WasCalled(Times.Once);
    }
}
