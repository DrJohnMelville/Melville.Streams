using Melville.IntersectionTypes.CodeGen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace Melville.IntersectionTypes.MixinGenerator;

internal readonly struct MixinCodeGenerator(
    INamedTypeSymbol parent,
    INamedTypeSymbol mixin,
    SourceProductionContext context)
{
    private readonly StringBuilder code = new();
    public void Emit() {
        if (parent.DeclaringSyntaxReferences[0].GetSyntax() is not TypeDeclarationSyntax tds) return;
        using (var _ = new ClassWrapper(code, tds, "", mixin.Interfaces))
        {
            new MethodForwardFacade(mixin, parent, $"(new {mixin.GlobalName}(this))", code).WriteMethods();
        }

        context.AddSource(FileName(), code.ToString());
    }
    string FileName() => FileNamer.FileNameFor(parent, mixin);
}