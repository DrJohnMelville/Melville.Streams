using Melville.IntersectionTypes.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Melville.IntersectionTypes.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AndTypeAnalyzer : DiagnosticAnalyzer
{

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [ErrorDeclarations.ParameterLacksType];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(CheckParameter, SyntaxKind.Argument);
    }
    
    private void CheckParameter(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is ArgumentSyntax argument &&
            context.SemanticModel.GetOperation(argument) is IArgumentOperation operation &&
            operation.Parent is IObjectCreationOperation { Arguments.Length : 1} constructorCall &&
            DesiredTypes(constructorCall.Type, context.Compilation) is { } desired &&
            GetExpressionType(operation.Value) is { } actual)
            new ParameterVerifier(actual, desired, context).Check();
      
    }
    
    private ITypeSymbol? GetSentinelType(Compilation compilation) =>
        compilation.GetTypeByMetadataName(typeof(IntersectionTypeAttribute).FullName);


    private IEnumerable<ITypeSymbol> DesiredTypes(ITypeSymbol? constructedType, 
        Compilation compilation)
    {
        if (constructedType is not null &&
            GetSentinelType(compilation) is { } sentinelType)
        {
            foreach (var attr in constructedType.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, sentinelType))
                    return constructedType.GetComponentTypes(compilation);
            }
        }
        return [];
    }

    ITypeSymbol? GetExpressionType(IOperation value) => value switch
    {
        IConversionOperation conv => GetExpressionType(conv.Operand),
        _ => value.Type
    };
}
