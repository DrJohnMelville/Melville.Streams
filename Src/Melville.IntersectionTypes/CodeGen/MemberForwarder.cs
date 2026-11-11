using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Melville.IntersectionTypes.CodeGen;

internal class MemberForwarder(
    string returnType,
    string body,
    string name,
    string? parameterTypes,
    ISymbol forwardedMethod) 
{
    private string name = name;
    private string? parameterTypes = parameterTypes;

    public void WriteImplicitForwarder(StringBuilder sb)
    {
        WriteDocComments(sb, forwardedMethod);
        sb.AppendLine($"    public {returnType} {body}");
    }
    private void WriteDocComments(StringBuilder sb, ISymbol symbol)
    {
        if (symbol.GetDocumentationCommentXml(null, true) is { Length: > 0 } comment)
        {
            CopyIntoCommentBlock(sb, comment);
        }
    }

    private static void CopyIntoCommentBlock(StringBuilder sb, string comment)
    {
        int startIndex = 0;
        while (startIndex < comment.Length)
        {
            // Split on '\n' so both LF and CRLF line endings work (a CRLF line keeps its '\r').
            var endIndex = comment.IndexOf('\n', startIndex);
            if (endIndex < 0) endIndex = comment.Length - 1;
            sb.Append("    ///");
            sb.Append(comment, startIndex, 1 + endIndex - startIndex);
            if (comment[endIndex] != '\n') sb.AppendLine();
            startIndex = endIndex + 1;
        }
    }

    public static IEqualityComparer<MemberForwarder> Comparer{ get; } = new SameMethod();

    private class SameMethod : IEqualityComparer<MemberForwarder>
    {
        public bool Equals(MemberForwarder x, MemberForwarder y)
        {
            if (!x.name.Equals(y.name, StringComparison.Ordinal)) return false;
            return (x.parameterTypes, y.parameterTypes) switch
            {
                (null, _) or (_, null) => true,
                var (a, b) => a.Equals(b, StringComparison.Ordinal)
            };
        }

        public int GetHashCode(MemberForwarder obj) => obj.name.GetHashCode();
    }
}
