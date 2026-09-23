using Melville.IntersectionTypes;

namespace Test.IntegrationTests;

public interface IA<T> {
    void Method(T t);
}




public partial class GenericIntersectionTest 
{
    [IntersectionType]
    public readonly partial struct Intersect<T>
    {
        partial void IsIntersectionOfTypes(IA<T> a, IB b);
    }

    [Test]
    public void Create()
    {
        var mock = Mock.Of<IA<string>,IB>();
        Intersect<string>.TryCreateFrom(mock.Object, out var sut);
        sut.Method("Hello World");
        mock.Method("Hello World").WasCalled(Times.Once);
    } 
}
