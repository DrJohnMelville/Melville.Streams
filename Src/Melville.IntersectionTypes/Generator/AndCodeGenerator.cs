using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Melville.IntersectionTypes.Generator;

public record struct AndCodeGenerator(StructDeclarationSyntax Declaration, SemanticModel SemanticModel)
{
    public void Generate(SourceProductionContext context)
    {
        ISymbol? symbol = SemanticModel.GetDeclaredSymbol(Declaration);
        if (symbol is null) throw new InvalidOperationException("Cannot find target symbol.");
        var components = symbol.GetComponentTypes();

        CheckComponentTypes(components);
        var engine = new InnerAndCodeGenerator(Declaration, symbol, components);

        context.AddSource(engine.TargetFileName(), engine.ImplementationCode());
    }

    private void CheckComponentTypes(ImmutableArray<TypedConstant> components)
    {
        if (components.Length < 2) 
            throw new InvalidOperationException("An IntersectionType must specify at least 2 types");
    }
}

public static class GetComponentTypesImpl
{
    extension (ISymbol symbol)
    {
        public ImmutableArray<TypedConstant> GetComponentTypes()
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

    }
 
    private static ImmutableArray<TypedConstant> GetComponentTypes(AttributeData attr) =>
        attr.ConstructorArguments[0].Values;
}
