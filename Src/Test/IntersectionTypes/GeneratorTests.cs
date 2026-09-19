using Melville.IntersectionTypes;
using Melville.IntersectionTypes.Generator;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VerifyTUnit;

namespace Test.IntersectionTypes;

public class GeneratorTests
{
    private static Task SuccessText(string code)
    {
        var tb = new GeneratorTestBed(new AndGenerator(), code);
        tb.AssertNoDiagnostics();
        tb.NoSuchFile("aaa.cs");

        return Verifier.Verify(tb.FromName("IA_IB.g.cs").Text());
    }

    [Test]
    public Task TwoInterfacesWithNoMembers() => SuccessText("""
            using Melville.IntersectionTypes;

            namespace NS.A.B;

            public interface IA {}
            public interface IB {}

            [IntersectionType(typeof(IA), typeof(IB))]
            public readonly partial struct IA_IB {}
 
            """);

    [Test] public Task ClassAndInterface() => SuccessText("""
        using Melville.IntersectionTypes;
        
        namespace NS.A.B;
        
        public class IA {}
        public interface IB {}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """);

    [Test] public Task InterfacesWithMemners() => SuccessText("""
        using Melville.IntersectionTypes;
                
        public interface IA {int A {get;}}
        public interface IB {System.String B(int i);}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """);

    [Test] public Task InterfacesGetAndSet() => SuccessText("""
        using Melville.IntersectionTypes;
                
        public interface IA {int A {get; set;}}
        public interface IB {}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """);


    [Test] public Task ClassWithPrivateSet() => SuccessText("""
        using Melville.IntersectionTypes;
                
        public class IA {public int A {get; private set;}}
        public interface IB {}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """);
    [Test] public Task ClassWithInitProperty() => SuccessText("""
        using Melville.IntersectionTypes;
                
        public class IA {public int A {get; init;}}
        public interface IB {}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """);

    [Test] public Task ClassWithPrivateGet() => SuccessText("""
        using Melville.IntersectionTypes;
                
        public class IA {public int A {private get; set;}}
        public interface IB {}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """);

    [Test] public Task ClassWithPrivateGetAndSet() => SuccessText("""
        using Melville.IntersectionTypes;
                
        public class IA {public int A {private get; private set;}}
        public interface IB {}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """);
    [Test] public Task ClassWithPrivateAndProtectedMethods() => SuccessText("""
        using Melville.IntersectionTypes;
                
        public class IA {private int Priv(); protected int Protected();}
        public interface IB {}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """);
    [Test] public Task MethodWithNoParameters() => SuccessText("""
        using Melville.IntersectionTypes;
                
        public interface IA { int Public();}
        public interface IB {}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """);
    [Test] public Task IndexerWithSetAndGet() => SuccessText("""
        using Melville.IntersectionTypes;
                
        public interface IA { int this[int i] {get; set;};}
        public interface IB {}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """);

    [Test] public Task EventWithSetAndRemove() => SuccessText("""
        using Melville.IntersectionTypes;
                
        public interface IA { event EventHandler AE;}
        public interface IB {}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """);

    [Test] public Task IgnoreClasses() => SuccessText("""
        using Melville.IntersectionTypes;
                
        public interface IA { class AC {}}
        public interface IB {}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """);

    [Test] public Task StaticMethods() => SuccessText("""
        using Melville.IntersectionTypes;
                
        public interface IA { public static int A();}
        public interface IB {}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """);

    [Test] public Task MethodWithMultipleParams() => SuccessText("""
        using Melville.IntersectionTypes;
                
        public interface IA { public int A(int a, string b, IA c, IB veryLongVariableName);}
        public interface IB {}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """);

    [Test] public Task MethodWithRefParameter() => SuccessText("""
        using Melville.IntersectionTypes;
                
        public interface IA { public int A(ref int a);}
        public interface IB {}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """);

    [Test] public Task MethodWithReadOnluRefParameter() => SuccessText("""
        using Melville.IntersectionTypes;
                
        public interface IA { public int A(ref readonly int a);}
        public interface IB {}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """);

    [Test] public Task MethodInParameter() => SuccessText("""
        using Melville.IntersectionTypes;
                
        public interface IA { public int A(in int a);}
        public interface IB {}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """);

    [Test] public Task MethodOutParameter() => SuccessText("""
        using Melville.IntersectionTypes;
                
        public interface IA { public int A(out int a);}
        public interface IB {}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """);

    [Test] public Task RefReturns() => SuccessText("""
        using Melville.IntersectionTypes;
                
        public interface IA { ref int A();}
        public interface IB {}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """);

    [Test] public Task VoidMethod() => SuccessText("""
        using Melville.IntersectionTypes;
                
        public interface IA { public void A();}
        public interface IB {}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """);

    [Test] public Task ParameterWithDefaultValue() => SuccessText("""
        using Melville.IntersectionTypes;
                
        public interface IA { public void A(int i = 10);}
        public interface IB {}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """);

    [Test] public Task ParameterWithDefaultStringValue() => SuccessText(""""
        using Melville.IntersectionTypes;
                
        public interface IA { public void A(string i = "hel\r\nlo");}
        public interface IB {}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """");

    [Test] public Task ParameterWithDefaultStructValue() => SuccessText(""""
        using Melville.IntersectionTypes;
                
        public readonly struct S (int i);
        public interface IA { public void A(S i = default);}
        public interface IB {}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """");

    [Test] public Task ParameterWithDefaultEnumValue() => SuccessText(""""
        using Melville.IntersectionTypes;
                
        public readonly enum S {A, B, C, D};
        public interface IA { public void A(S i = S.C);}
        public interface IB {}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """");

    [Test] public Task RefProperty() => SuccessText(""""
        using Melville.IntersectionTypes;
                
        public readonly enum S {A, B, C, D};
        public interface IA { ref int I {get;}}
        public interface IB {}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """");

    [Test] public Task CommonProperties() => SuccessText(""""
        using Melville.IntersectionTypes;
                
        public readonly enum S {A, B, C, D};
        public interface IA { int I  {get;}}
        public interface IB { int I {get;}}
        
        [IntersectionType(typeof(IA), typeof(IB))]
        public readonly partial struct IA_IB {}
        """");
}
