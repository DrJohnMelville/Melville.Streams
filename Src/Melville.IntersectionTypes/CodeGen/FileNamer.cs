using Microsoft.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Melville.IntersectionTypes.CodeGen;

public static class FileNamer
{
    public static string FileNameFor(ISymbol sym) =>
                ReplaceNonFileChars($"""{sym.CSharpName}\{sym.Name}.g.cs""");
    public static string FileNameFor(ISymbol sym, ISymbol sym2) =>
                ReplaceNonFileChars($"""{sym.CSharpName}\{sym.Name}_{sym2.Name}.g.cs""");

    private static string ReplaceNonFileChars(string s) =>
        Regex.Replace(s, @"[<>]", "_");
}
