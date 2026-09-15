using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Melville.IntersectionTypes;


public readonly struct InnerAndCodeGenerator(
        StructDeclarationSyntax declaration,
        ISymbol symbol,
        ImmutableArray<TypedConstant> interfaces
    )
{
    private readonly StringBuilder sb = new();

    public string TargetFileName() =>
        $"""{symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)}\{symbol.Name}.g.cs""";

    public string ImplementationCode()
    {
        sb.AppendLine($$"""
        [global::System.Runtime.CompilerServices.UnionAttribute]
        {{declaration.Modifiers}} struct {{symbol.GlobalName}}:
        """);
        DeclareAncestors();
        sb.AppendLine("{");
        DeclareConstructor();
        DeclareMethodForwarders();
        sb.AppendLine("}");

        return sb.ToString();
    }

    private void DeclareAncestors()
    {
        var first = true;
        foreach (var inter in interfaces)
        {
            if (inter.Value is not INamedTypeSymbol { TypeKind: TypeKind.Interface }) continue;
            if (!first)
            {
                sb.Append(", ");
            }
            else
            {
                first = false;
            }
            sb.Append((inter.Value as ISymbol).GlobalName);
        }

        sb.AppendLine();
    }
    void DeclareConstructor()
    {
        if (interfaces[0].Value is ISymbol first)
        {
            sb.AppendLine($$"""
                private readonly {GlobalName(first)} _value;
                public {symbol.Name} ({GlobalName(first)} value) 
                {
                    _value = value;
                    global::Melville.IntersectionTypes.TypeVerifier.VerifyTypes(_value
            """);
            foreach (var child in interfaces.Skip(1))
            {
                sb.AppendLine($"            , typeof({(child.Value as ISymbol).GlobalName})");
            }
            sb.AppendLine("""
                    );
                }
            """);

        }
    }
    void DeclareMethodForwarders()
    {
        foreach (var inter in interfaces)
        {
            if (inter.Value is INamedTypeSymbol sym)
                DeclareComponentForwarders(sym);
        }
    }

    // we don't generate the component methods of properties, events, or indexers, because
    // we generate those using higher level constructs
    private bool IsNotSpecialInternalMethod(ISymbol i) => i.CanBeReferencedByName ||
        i is IPropertySymbol { IsIndexer: true };
 
    void DeclareComponentForwarders(INamedTypeSymbol sym)
    {
        sb.AppendLine();
        GenerateAsMethod(sym);
        DeclareClassImplicitOperator(sym);

        GenerateAllMembers(sym);
    }

    private void GenerateAllMembers(INamedTypeSymbol sym)
    {
        var fact = new MemberForwarderFactory(sym.Name);
        foreach (var member in sym.GetMembers())
        {
            if (IsNotSpecialInternalMethod(member))
                fact.Create(member)?.WriteImplicitForwarder(sb);
        }
    }

    private void GenerateAsMethod(INamedTypeSymbol sym) =>
        sb.AppendLine($"""
                public {sym.GlobalName} As{sym.Name}() => 
                    ({sym.GlobalName})_value;
            """);

    private void DeclareClassImplicitOperator(INamedTypeSymbol sym)
    {
        if (sym.TypeKind is not TypeKind.Interface)
        {
            sb.AppendLine($"""
                    public static implicit {sym.GlobalName}({symbol.GlobalName} i) => 
                        i.As{sym.Name}
                """);

        }
    }
}

public static class SymbolOperations
{
    extension (ISymbol? sym)
    {
        public string GlobalName => sym?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ??
           throw new InvalidOperationException("Cannot find symbol name");
    }
}