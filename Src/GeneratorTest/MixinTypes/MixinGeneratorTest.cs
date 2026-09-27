using Melville.IntersectionTypes.MixinGenerator;
using System.Threading.Tasks;
using Test.IntersectionTypes;
using VerifyTUnit;

namespace GeneratorTest.MixinTypes;

internal class MixinGeneratorTest
{
    protected static Task SuccessText(string code)
    {
        var tb = new GeneratorTestBed(new MixinGenerator(), code);
        tb.AssertNoDiagnostics();

        return Verifier.Verify(tb.FromName("Test_Mix.g.cs").Text());
    }

    [Test]
    public Task EmptyMixin() => SuccessText("""
        using Melville.IntersectionTypes;
        
        public readonly struct Mix{}

        public partial class Test: Melville.IntersectionTypes.IMixin<Mix> {
        }
        """);

    [Test]
    public Task MixinWithInterface() => SuccessText("""
        using Melville.IntersectionTypes;

        public interface IMix {}
        
        public readonly struct Mix:IMix{}

        public partial class Test: Melville.IntersectionTypes.IMixin<Mix> {
        }
        """);

    [Test]
    public Task ForwardMethod0() => SuccessText("""
        using Melville.IntersectionTypes;

        public readonly struct Mix{
             public float Method(ref int A, out string y);
        }

        public partial class Test: Melville.IntersectionTypes.IMixin<Mix> {
        }
        """);

}
