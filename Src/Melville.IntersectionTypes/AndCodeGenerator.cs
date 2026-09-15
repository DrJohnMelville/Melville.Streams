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

    private ImmutableArray<TypedConstant> GetComponentTypes(ISymbol symbol)
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
    
    private ImmutableArray<TypedConstant> GetComponentTypes(AttributeData attr) =>
        attr.ConstructorArguments[0].Values;
    
    private void CheckComponentTypes(ImmutableArray<TypedConstant> components)
    {
        if (components.Length < 2) 
            throw new InvalidOperationException("An IntersectionType must specify at least 2 types");
    }
}

