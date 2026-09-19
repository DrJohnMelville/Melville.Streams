using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Melville.IntersectionTypes.Generator;

[Generator]
public class AndGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(
            context.SyntaxProvider.ForAttributeWithMetadataName(
                typeof(IntersectionTypeAttribute).FullName,
                IsAcceptableNodeType,
                CodeGeneratorFactory
                ),
            ExecuteGenerator);
    }
    bool IsAcceptableNodeType(SyntaxNode node, CancellationToken cancel) =>
        node is StructDeclarationSyntax;
 
    private AndCodeGenerator CodeGeneratorFactory(GeneratorAttributeSyntaxContext context, CancellationToken token) =>
        new AndCodeGenerator((StructDeclarationSyntax)context.TargetNode, context.SemanticModel);
    
    void ExecuteGenerator(SourceProductionContext context, AndCodeGenerator generator) =>
        generator.Generate(context);
}
