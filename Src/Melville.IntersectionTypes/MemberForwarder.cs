using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Xml.Linq;

namespace Melville.IntersectionTypes;

public class MemberForwarder(
    string returnType,
    string body,
    string name,
    string? parameterTypes,
    string parentType) 
{
    private string name = name;
    private string parameterTypes = parameterTypes;

    public void WriteImplicitForwarder(StringBuilder sb)
    {
        sb.AppendLine($"    public {returnType} {body}");
    }

    public void WriteExplicitForwarder(StringBuilder sb)
    {
        sb.AppendLine($"    {returnType} {parentType}.{body}");
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