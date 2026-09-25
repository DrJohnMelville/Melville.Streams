using Melville.IntersectionTypes.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Melville.IntersectionTypes.CodeGen;

public class ClassWrapper : IDisposable
{
    public void Dispose() => sb.Append(new string('}', levels));
    private readonly StringBuilder sb;
    private readonly int levels;

    public ClassWrapper(StringBuilder sb, TypeDeclarationSyntax tds, string classAttributes,
        IEnumerable<ISymbol> interfaces)
    {
        this.sb = sb;
        levels = WriteTypeDeclaration(tds, () => DeclareAncestors(interfaces), classAttributes);
    }

    private int WriteTypeDeclaration(TypeDeclarationSyntax syntax, Action? writeParents, string claassAttributes)
    {
        var ret = WriteWrapper(syntax.Parent);
        if (!string.IsNullOrWhiteSpace(claassAttributes)) sb.AppendLine(claassAttributes);
        sb.Append($"{syntax.Modifiers} {syntax.Keyword} {syntax.Identifier}{syntax.TypeParameterList}");
        writeParents?.Invoke();
        sb.AppendLine();
        sb.AppendLine("{");
        return ret + 1;
    }

    private int WriteWrapper(SyntaxNode? token)
    {

        switch (token)
        {
            case BaseNamespaceDeclarationSyntax ns:
                sb.AppendLine($"namespace {ns.Name};");
                return 0;
            case TypeDeclarationSyntax td:
                return WriteTypeDeclaration(td, null, "");
            case CompilationUnitSyntax:
            case null: return 0;

        }
        throw new InvalidDataException($"""Cannot handle containter "{token}".""");
    }

    private void DeclareAncestors(IEnumerable<ISymbol> interfaces)
    {
        var delim = new FirstDifferenceBuffer<string>(":\r\n", ",\r\n");
        foreach (var inter in interfaces)
        {
            sb.Append(delim.Next());
            sb.Append(inter.GlobalName);
        }
    }


}