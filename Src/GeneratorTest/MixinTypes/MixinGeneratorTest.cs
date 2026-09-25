using Melville.IntersectionTypes.Generator;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Test.IntersectionTypes;
using VerifyTUnit;

namespace GeneratorTest.MixinTypes;

internal class MixinGeneratorTest
{
    protected static Task SuccessText(string code)
    {
        var tb = new GeneratorTestBed(new AndGenerator(), code);
        tb.AssertNoDiagnostics();

        return Verifier.Verify(tb.FromName("IA_IB.g.cs").Text());
    }

    [Test]
    public Task NoMixins() => SuccessText("""
        using Melville.IntersectionTypes;
        
        public partial class IA_IB {

        }
        """);

}
