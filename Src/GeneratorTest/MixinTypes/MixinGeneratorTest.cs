using Melville.IntersectionTypes.Generator;
using Melville.IntersectionTypes.MixinGenerator;
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
        var tb = new GeneratorTestBed(new MixinGenerator(), code);
        tb.AssertNoDiagnostics();

        return Verifier.Verify(tb.FromName("Test_Mix.g.cs").Text());
    }

    [Test]
    public Task NoMixins() => SuccessText("""
        using Melville.IntersectionTypes;
        
        public readonly struct Mix{}

        public partial class Test_Mix: Melville.IntersectionTypes.IMixin<Mix> {
        }
        """);

}
