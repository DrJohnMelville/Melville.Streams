using Melville.IntersectionTypes;
using Melville.IntersectionTypes.Analyzer;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;

namespace GemeratorTest.IntersectionTypes;

public class AnalyzerTest
{
    CSharpAnalyzerTest<AndTypeAnalyzer, DefaultVerifier> verifier = new();

    private async Task RunDiagnostic(string code, string? arg1 = null, string? arg2 = null)
    {
        verifier.TestCode = code;
        verifier.SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId);
            var parseOptions = (CSharpParseOptions)project.ParseOptions;

            // Override the language version to Preview
            return solution.WithProjectParseOptions(
                projectId,
                parseOptions.WithLanguageVersion(LanguageVersion.Preview)
            );
        });

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
        [Melville.IntersectionTypes.IntersectionType()]
        public partial struct Both: IA, IB {
            partial void IsIntersectionOfTypes(IA ia, IB ib);
                    public Both( IA i){}
        }
        public class A 
        {
            public object Method() => new Both((IA)new Inner());
        }
        """);

    [Test]
    public Task IntersectionWithInterfaceAndClassSuccess() => RunDiagnostic("""
        public interface IA{}
        public class IB{}
        public class Inner: IB, IA{}
        [Melville.IntersectionTypes.IntersectionType]
        public partial struct Both: IA {
            partial void IsIntersectionOfTypes(IA ia, IB ib);
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
        [Melville.IntersectionTypes.IntersectionType]
        public partial struct Both: IA, IB {
                            public Both( IA i){}
        }
        public class A 
        {
            public object Method() => new Both([|new Inner()|]);
        }
        """, "Foo", "Bar");


    [Test]
    public Task CheckImplicitObjectCreation() => RunDiagnostic("""
        namespace System.Runtime.CompilerServices;

        public class UnionAttribute: Attribute {}

        public interface IA{}
        public interface IB{}
        public class Inner: IA{}
        [System.Runtime.CompilerServices.Union]
        [Melville.IntersectionTypes.IntersectionType]
        public partial struct Both: IA, IB {
                            public Both( IA i){}
                            public object Value {get;}
        }
        public class A 
        {
            private void M2(Both _) {}
            public void Method() {
                 M2([|new Inner()|]);
            }
        }
        """, "Foo", "Bar"); 
    
    [Test]
    public Task CheckImplicitObjectCreationSucceed() => RunDiagnostic("""
        namespace System.Runtime.CompilerServices;

        public class UnionAttribute: Attribute {}

        public interface IA{}
        public interface IB{}
        public class Inner: IA, IB{}
        [System.Runtime.CompilerServices.Union]
        [Melville.IntersectionTypes.IntersectionType]
        public partial struct Both: IA, IB {
                            public Both( IA i){}
                            public object Value {get;}
        }
        public class A 
        {
            private void M2(Both _) {}
            public void Method() {
                 M2(new Inner());
            }
        }
        """, "Foo", "Bar");
}