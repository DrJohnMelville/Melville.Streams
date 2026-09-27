using Melville.IntersectionTypes.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
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
        context.RegisterOperationAction(CheckOperation, OperationKind.Conversion);
    }
    void CheckOperation(OperationAnalysisContext context)
    {
        if (context.Operation is IConversionOperation operation &&
            operation.Type is { } desired &&
            operation.Operand.Type is { } actual &&
            new ParameterVerifier(actual, desired, context.Compilation).Check() is { } desiredInterface
            )
        {
            context.ReportDiagnostic(
               Diagnostic.Create(ErrorDeclarations.ParameterLacksType,
                context.Operation.Syntax.GetLocation(),
                 actual.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                desiredInterface?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
        }
    }

    private void CheckParameter(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is ArgumentSyntax argument &&
            context.SemanticModel.GetOperation(argument) is IArgumentOperation operation &&
            operation.Parent is IObjectCreationOperation { Arguments.Length : 1} constructorCall &&
            GetExpressionType(operation.Value) is { } actual &&
            new ParameterVerifier(actual, constructorCall.Type, context.Compilation).Check() is { } desiredInterface)
        {
            context.ReportDiagnostic(
               Diagnostic.Create(ErrorDeclarations.ParameterLacksType,
                context.Node.GetLocation(),
                 actual.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                desiredInterface?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));

        }

    }
    ITypeSymbol? GetExpressionType(IOperation value) => value switch
    {
        IConversionOperation conv => GetExpressionType(conv.Operand),
        _ => value.Type
    };
}
