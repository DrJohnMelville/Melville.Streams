using Melville.IntersectionTypes;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VerifyTUnit;

namespace Test.IntersectionTypes;

public class GeneratorTests
{
    [Test]
    public Task TwoInterfacesWithNoMembers()
    {
        var tb = new GeneratorTestBed(new AndGenerator(), """
            using Melville.IntersectionTypes;

            namespace NS.A.B;

            public interface IA {}
            public interface IB {}

            [IntersectionType(typeof(IA), typeof(IB))]
            public readonly partial struct IA_IB {}
 
            """);
        tb.AssertNoDiagnostics();
        tb.NoSuchFile("aaa.cs");

        return Verifier.Verify(tb.FromName("IA_IB.g.cs").Text());
    }
}
