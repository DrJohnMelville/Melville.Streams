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
    private const string ruleName = "And0001";
    public static readonly DiagnosticDescriptor Rule = new(
        id: ruleName,
        title: "Invalid intersecion type arguments",
        messageFormat: "Type {0} does not implement {1}",
        category: "Types",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
        );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [Rule];

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
            DesiredTypes(constructorCall.Type) is { Length: > 1 } desired &&
            GetExpressionType(operation.Value) is { } actual)
            new ParameterVerifier(actual, desired, context).Check();
      
    }
    private ImmutableArray<TypedConstant> DesiredTypes(ITypeSymbol? constructedType) => constructedType is not null ?
            constructedType.GetComponentTypes() : [];

    ITypeSymbol? GetExpressionType(IOperation value) => value switch
    {
        IConversionOperation conv => GetExpressionType(conv.Operand),
        _ => value.Type
    };
}
