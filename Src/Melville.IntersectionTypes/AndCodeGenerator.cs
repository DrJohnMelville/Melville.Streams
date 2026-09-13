using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Melville.IntersectionTypes;

public record struct AndCodeGenerator(StructDeclarationSyntax Declaration, SemanticModel SemanticModel)
{
    public void Generate(SourceProductionContext context)
    {
        ISymbol? symbol = SemanticModel.GetDeclaredSymbol(Declaration);
        if (symbol is null) throw new InvalidOperationException("Cannot find target symbol.");
        var components = GetComponentTypes(symbol);

        CheckComponentTypes(components);
        var engine = new InnerAndCodeGenerator(Declaration, symbol, components);

        context.AddSource(engine.TargetFileName(), engine.ImplementationCode());
    }

    ImmutableArray<TypedConstant> GetComponentTypes(ISymbol symbol)
    {
        var attrs = symbol.GetAttributes();
        foreach (var attr in attrs)
        {
            var className = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if ($"global::{typeof(IntersectionTypeAttribute).FullName}"
                .Equals(className, StringComparison.Ordinal))
                return GetComponentTypes(attr);
        }
        return [];
    }
    ImmutableArray<TypedConstant> GetComponentTypes(AttributeData attr)
    {
        return attr.ConstructorArguments[0].Values;
       // var firstValue = values[0];
     //   var symbol = firstValue.Value as INamedTypeSymbol;
     //   return [];
    }
    void CheckComponentTypes(ImmutableArray<TypedConstant> components)
    {
        if (components.Length < 2) 
            throw new InvalidOperationException("An IntersectionType must specify at least 2 types");
    }
}


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
        {{declaration.Modifiers}} struct {{GlobalName(symbol)}}:
        """);
        DeclareAncestors();
        sb.AppendLine("{");
        DeclareConstructor();
        DeclareMethodForwarders();
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GlobalName(ISymbol? sym) =>
        sym?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ??
           throw new InvalidOperationException("Cannot find symbol name");

    private void DeclareAncestors()
    {
        var first = true;
        foreach (var inter in interfaces)
        {
            if (!first)
            {
                sb.Append(", ");
            }
            else
            {
                first = false;
            }
            sb.Append(GlobalName(inter.Value as ISymbol));
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
                sb.AppendLine($"            , typeof({GlobalName(child.Value as ISymbol)})");
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
            if (inter.Value is ISymbol sym)
                DeclareMethodForwarder(sym);
        }
    }
    void DeclareMethodForwarder(ISymbol sym)
    {

        sb.AppendLine();
        sb.AppendLine($"    // Implement {sym.Name} forwarders");

        sb.AppendLine($"    public {GlobalName(sym)} As{sym.Name}() => ({GlobalName(sym)})_value;");
    }

}