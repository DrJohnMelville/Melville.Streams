using Melville.IntersectionTypes.CodeGen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Melville.IntersectionTypes.Generator;

internal readonly partial struct InnerAndCodeGenerator(
        INamedTypeSymbol hostSymbol,
        IList<ITypeSymbol> interfaces,
        Compilation compilation
    )
{
    private readonly StringBuilder sb = new();
    private readonly List<MemberForwarder> forwarders = new();

    public string TargetFileName() => FileNamer.FileNameFor(hostSymbol);

    public string ImplementationCode()
    {
        var syntax = (hostSymbol.DeclaringSyntaxReferences[0].GetSyntax() as
            TypeDeclarationSyntax);
        if (syntax is null) return "// no code generated for non member declaration";

        if (compilation.GetTypeByMetadataName(typeof(IIntersection).FullName) is not { } intersectionInterface)
            throw new InvalidOperationException("Could not find IIntersection interface.");

        using (var _ = new ClassWrapper(sb, syntax, "[System.Runtime.CompilerServices.Union]",
            interfaces.Where(i => i is INamedTypeSymbol { TypeKind: TypeKind.Interface }).
            Append(intersectionInterface)))
        {
            ClassContents();
        }

        return sb.ToString();
    }


    private void ClassContents()
    {
        DeclareConstructor();
        DeclareTryFactory();
        DeclareMethodForwarders();
    }

    void DeclareConstructor()
    {
        DeclareTypeList();

        if (FirstType() is not { } first) return;
        sb.AppendLine($$"""
                public object Value {get;}

                public {{hostSymbol.Name}} ({{first.GlobalName}} value): 
                    this(global::Melville.IntersectionTypes.TypeVerifier.Verify(value,
                    _requiredTypes().Slice(1)), true) {}

                private {{hostSymbol.Name}} (object value, bool verify) => Value = value;

            """);
    }

    private void DeclareTypeList()
    {
        sb.AppendLine("""
                private static global::System.ReadOnlySpan<global::System.Type> _requiredTypes() =>
                (global::System.Type[])[
            """);
        foreach (var child in interfaces)
        {
            sb.AppendLine($"        typeof({child.GlobalName}),");
        }
        sb.AppendLine("    ];");
    }

    private INamedTypeSymbol? FirstType() => interfaces[0] as INamedTypeSymbol;

    void DeclareTryFactory()
    {
        if (FirstType() is not { } first) return;
        sb.Append($$"""
                    public static bool TryCreateFrom(object input, 
                        out {{((ITypeSymbol)hostSymbol).GlobalName}} value)
                    {
                        if (global::Melville.IntersectionTypes.TypeVerifier.TryVerifyType(input,
                                 _requiredTypes(), out var verified)) 
                        {
                            value = new (verified, false );
                            return true;
                        }
                        else 
                        {
                            value = default!;
                            return false;
                        }
                    }
            """);
    }

    void DeclareMethodForwarders()
    {
        foreach (var inter in interfaces)
        {
            if (inter is INamedTypeSymbol componentSymbol)
            {
                DeclareComponentForwarders(componentSymbol);
                GenerateAllMembers(componentSymbol);
            }
        }
    }


    void DeclareComponentForwarders(INamedTypeSymbol componentSymbol)
    {
        sb.AppendLine();
        sb.AppendLine($"// Forwarers for {componentSymbol.Name}");
        GenerateAsMethod(componentSymbol);
        DeclareClassImplicitOperator(componentSymbol);

    }

    private void GenerateAsMethod(INamedTypeSymbol componentSymbol) =>
        sb.AppendLine($"""
                public {componentSymbol.GlobalName} As{componentSymbol.Name}() => 
                    ({componentSymbol.GlobalName})this.Value;
            """);

    private void DeclareClassImplicitOperator(INamedTypeSymbol componentSymbol)
    {
        if (componentSymbol.TypeKind is not TypeKind.Interface)
        {
            sb.AppendLine($"""
                    public static implicit {componentSymbol.GlobalName}({hostSymbol.GlobalName} i) => 
                        i.As{componentSymbol.Name}();
                """);

        }
    }

    private void GenerateAllMembers(INamedTypeSymbol componentSymbol)
    {
        new MethodForwardFacade(componentSymbol, hostSymbol, $"As{componentSymbol.Name}()", sb).WriteMethods();
    }
}
