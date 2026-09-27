using Melville.IntersectionTypes.CodeGen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Melville.IntersectionTypes.Generator;

public readonly partial struct InnerAndCodeGenerator(
        INamedTypeSymbol hostSymbol,
        IList<ITypeSymbol> interfaces
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

        using (var _ = new ClassWrapper(sb, syntax, "[System.Runtime.CompilerServices.Union]",
            interfaces.Where(i => i is INamedTypeSymbol { TypeKind: TypeKind.Interface })))
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

                public {{hostSymbol.Name}} ({{first.GlobalName}} value): this(value, true) =>
                    global::Melville.IntersectionTypes.TypeVerifier.VerifyTypes(Value,
                    _requiredTypes().Slice(1));
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
                        if (global::Melville.IntersectionTypes.TypeVerifier.IsValidType(input,
                               _requiredTypes())) 
                        {
                            value = new (input, false );
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
