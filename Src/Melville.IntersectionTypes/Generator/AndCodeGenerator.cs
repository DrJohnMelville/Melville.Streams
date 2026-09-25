using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;

namespace Melville.IntersectionTypes.Generator;

public record struct AndCodeGenerator(StructDeclarationSyntax Declaration, SemanticModel SemanticModel)
{
    public void Generate(SourceProductionContext context)
    {
        var symbol = SemanticModel.GetDeclaredSymbol(Declaration) as INamedTypeSymbol;
        if (symbol is null) throw new InvalidOperationException("Cannot find target symbol.");
        var components = symbol.GetComponentTypes().ToArray();

        if (components.Length < 2)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                            ErrorDeclarations.NeedTwoOrMoreTypes,
                            Declaration.GetLocation(),
                            symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                            ));
            return;
        }
        var engine = new InnerAndCodeGenerator(symbol, components);

        string name = engine.TargetFileName();
        string source = engine.ImplementationCode();

        context.AddSource(name, source);
    }

    private void CheckComponentTypes(
        ImmutableArray<IParameterSymbol> components, SourceProductionContext context, INamedTypeSymbol symbol)
    {
        if (components.Length < 2)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ErrorDeclarations.NeedTwoOrMoreTypes,
                Declaration.GetLocation(),
                symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                ));
        }
            
    }
}

public static class GetComponentTypesImpl
{
    extension (ITypeSymbol symbol)
    {
        public IEnumerable<ITypeSymbol> GetComponentTypes()
        {

            foreach (var parent in symbol.Interfaces)
                yield return ExtractType(parent);
        }
    }

    private static ITypeSymbol ExtractType(ITypeSymbol symbol) =>
        (symbol is INamedTypeSymbol nts &&
            symbol.GlobalName.StartsWith("global::Melville.IntersectionTypes.IIntersectionClass<") &&
            nts.TypeArguments.Length is 1)? nts.TypeArguments[0]: symbol;
}
