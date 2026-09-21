using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace Melville.IntersectionTypes.Analyzer;

public readonly struct ParameterVerifier(
    ITypeSymbol argumentType,
    ImmutableArray<IParameterSymbol> interfaces,
    SyntaxNodeAnalysisContext context)
{
    public void Check()
    {
        for (int i = 1; i < interfaces.Length; i++)
        {
            CheckSingleType(interfaces[i].Type);
        }
    }

    private void CheckSingleType(ITypeSymbol? desiredInterface)
    {
        if (!ArgumentMatchesType(desiredInterface))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(ErrorDeclarations.ParameterLacksType,
                    context.Node.GetLocation(),
                    argumentType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    desiredInterface?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
        }
    }

    private bool ArgumentMatchesType(ITypeSymbol? desiredInterface)
    {
        if (desiredInterface is null) return true;
        if (context.Compilation.HasImplicitConversion(argumentType, desiredInterface))
            return true ;
        foreach (var parent in argumentType.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(desiredInterface, parent)) return true;
        }
        return false;

    }
}