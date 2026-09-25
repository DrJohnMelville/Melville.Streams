using Melville.IntersectionTypes.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

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
            DesiredTypes(constructorCall.Type) is { } desired &&
            GetExpressionType(operation.Value) is { } actual)
            new ParameterVerifier(actual, desired, context).Check();
      
    }
    private IEnumerable<ITypeSymbol> DesiredTypes(ITypeSymbol? constructedType)
    {

        if (constructedType is not null)
        {
            foreach (var attr in constructedType.GetAttributes())
            {
                if (attr.AttributeClass.GlobalName is 
                    "global::Melville.IntersectionTypes.IntersectionTypeAttribute")
                    return constructedType.GetComponentTypes();
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
