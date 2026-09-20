using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Melville.IntersectionTypes;
using Melville.IntersectionTypes.Analyzer;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Test.IntersectionTypes;

public class AnalyzerTest
{
    CSharpAnalyzerTest<AndTypeAnalyzer, DefaultVerifier> verifier = new();

    private async Task RunDiagnostic(string code, string? arg1 = null, string? arg2 = null)
    {
        verifier.TestCode = code;
        verifier.TestState.AdditionalReferences.Add(typeof(IntersectionTypeAttribute).Assembly);
        
        await verifier.RunAsync();
    }

    [Test]
    public Task NoDiagnosticForUnrelatedCode() => RunDiagnostic("""
            class A {
                public int Meth() => 1; 
            }
            """);

    [Test]
    public Task IntersectionWithTwoTypesSuccess() => RunDiagnostic("""
        public interface IA{}
        public interface IB{}
        public class Inner: IA, IB{}
        [Melville.IntersectionTypes.IntersectionType(typeof(IA),typeof(IB))]
        public struct Both: IA, IB {
            public Both( IA i){}
        }
        public class A 
        {
            public object Method() => new Both(new Inner());
        }
        """);

    [Test]
    public Task IntersectionWithInterfaceAndClassSuccess() => RunDiagnostic("""
        public interface IA{}
        public class IB{}
        public class Inner: IB, IA{}
        [Melville.IntersectionTypes.IntersectionType(typeof(IA),typeof(IB))]
        public struct Both: IA {
            public Both( IA i){}
        }
        public class A 
        {
            public object Method() => new Both(new Inner());
        }
        """);

    [Test]
    public Task SecondTypeMissing() => RunDiagnostic("""
        public interface IA{}
        public interface IB{}
        public class Inner: IA{}
        [Melville.IntersectionTypes.IntersectionType(typeof(IA),typeof(IB))]
        public struct Both: IA, IB {
            public Both( IA i){}
        }
        public class A 
        {
            public object Method() => new Both([|new Inner()|]);
        }
        """, "Foo", "Bar");
}