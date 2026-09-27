using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;

namespace Melville.IntersectionTypes.MixinGenerator;

[Generator]
public class MixinGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(
                context.SyntaxProvider.CreateSyntaxProvider(Selector, Transform),
                Generate
            );
    }
    bool Selector(SyntaxNode node, CancellationToken camcel)
    {
        if (node is not GenericNameSyntax { Identifier.ValueText: "IMixin"} gns) return false;
        return GetParent<BaseListSyntax>(gns) is not null;
    }

    private T? GetParent<T>(SyntaxNode node) where T : SyntaxNode
    {
        for (SyntaxNode? item = node; item != null; item = item.Parent)
        {
            if (item is T ret) return ret;
        }
        return null;
    }
    private MixinGenerationRequest Transform(GeneratorSyntaxContext context, CancellationToken cancelation)
    {
        return  new MixinGenerationRequest(ParentSymbol(context), MixinType(context, cancelation));
    }

    private INamedTypeSymbol? MixinType(GeneratorSyntaxContext context, CancellationToken cancelation)
    {
        var wrapperSymbol = context.SemanticModel.GetTypeInfo(context.Node, cancelation).Type
            as INamedTypeSymbol;

        return (SymbolEqualityComparer.Default.Equals(wrapperSymbol?.ConstructedFrom, CorrectInterfaceReference(context)))? 
           wrapperSymbol?.TypeArguments[0] as INamedTypeSymbol: null;
    }

    private static INamedTypeSymbol? CorrectInterfaceReference(GeneratorSyntaxContext context)
    {
        return context.SemanticModel.Compilation.GetTypeByMetadataName(
                    typeof(IMixin<>).FullName);
    }

    private INamedTypeSymbol? ParentSymbol(GeneratorSyntaxContext context) =>
        (GetParent<TypeDeclarationSyntax>(context.Node) is not { } containerSyntax)? null :
         context.SemanticModel.GetDeclaredSymbol(containerSyntax) as INamedTypeSymbol;

    void Generate(SourceProductionContext context, MixinGenerationRequest request)
    {
        if (request is ({ } parent, { } mixin))
            new MixinCodeGenerator(parent, mixin, context).Emit();
    }
}

public record struct MixinGenerationRequest(INamedTypeSymbol? Parent, INamedTypeSymbol? Mixin)
{

}
