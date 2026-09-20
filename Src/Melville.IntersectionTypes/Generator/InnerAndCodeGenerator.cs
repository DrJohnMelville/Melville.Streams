using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Melville.IntersectionTypes.Generator;


public readonly partial struct InnerAndCodeGenerator(
        ISymbol symbol,
        ImmutableArray<TypedConstant> interfaces
    )
{
    private readonly StringBuilder sb = new();
    private readonly List<MemberForwarder> forwarders = new();

    public string TargetFileName() =>
        ReplaceNonFileChars(
        $"""{symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)}\{symbol.Name}.g.cs""");

    private string ReplaceNonFileChars(string s) =>
        Regex.Replace(s, @"[<>]", "_");

    public string ImplementationCode()
    {
        var syntax = (symbol.DeclaringSyntaxReferences[0].GetSyntax() as
            TypeDeclarationSyntax);
        if (syntax is null) return "// no code generated for non member declaration";

        var levels = WriteTypeDeclaration(syntax, DeclareAncestors);
        ClassContents();
        sb.Append(new string('}', levels));

        return sb.ToString();
    }

    private int WriteTypeDeclaration(TypeDeclarationSyntax syntax, Action? writeParents)
    {
        var ret = WriteWrapper(syntax.Parent);
        sb.Append($"{syntax.Modifiers} {syntax.Keyword} {syntax.Identifier}");
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
                return WriteTypeDeclaration(td, null);
            case CompilationUnitSyntax:
            case null: return 0;

        }
        throw new InvalidDataException($"""Cannot handle containter "{token}".""");
    }


    private void ClassContents()
    {
        DeclareConstructor();
        DeclareTryFactory();
        DeclareMethodForwarders();
        OutputForwarders();
    }

    private void DeclareAncestors()
    {
        var delim = new FirstDifferenceBuffer<string>(":\r\n", ",\r\n");
        foreach (var inter in interfaces)
        {
            if (inter.Value is not INamedTypeSymbol { TypeKind: TypeKind.Interface }) continue;
            sb.Append(delim.Next());
            sb.Append((inter.Value as ISymbol).GlobalName);
        }
    }
    void DeclareConstructor()
    {
        if (FirstType() is not { } first) return;

        sb.AppendLine("""
                private static global::System.ReadOnlySpan<global::System.Type> _requiredTypes() =>
                (global::System.Type[])[
            """);
        foreach (var child in interfaces)
        {
            sb.AppendLine($"        typeof({(child.Value as ISymbol).GlobalName}),");
        }
        sb.AppendLine("    ];");

        sb.AppendLine($$"""
                public {{first.GlobalName}} Value {get;}

                public {{symbol.Name}} ({{first.GlobalName}} value): this(value, true) =>
                    global::Melville.IntersectionTypes.TypeVerifier.VerifyTypes(Value,
                    _requiredTypes().Slice(1));
                private {{symbol.Name}} ({{first.GlobalName}} value, bool verify) =>
                    Value = value;
            """);
    }

    private INamedTypeSymbol? FirstType() => interfaces[0].Value as INamedTypeSymbol;

    void DeclareTryFactory()
    {
        if (FirstType() is not { } first) return;
        sb.Append($$"""
                    public static bool TryCreateFrom(object input, 
                        out {{((ITypeSymbol)symbol).GlobalName}} value)
                    {
                        if (global::Melville.IntersectionTypes.TypeVerifier.IsValidType(input,
                               _requiredTypes())) 
                        {
                            value = new (({{first.GlobalName}}) input, false );
                            return true;
                        }
                        else 
                        {
                            value = default!;
                            return false;
                        }
                    }
            """);
    }

    void DeclareMethodForwarders()
    {
        foreach (var inter in interfaces)
        {
            if (inter.Value is INamedTypeSymbol sym)
                DeclareComponentForwarders(sym);
        }
    }

    // we don't generate the component methods of properties, events, or indexers, because
    // we generate those using higher level constructs
    private bool IsNotSpecialInternalMethod(ISymbol i) => i.CanBeReferencedByName ||
        i is IPropertySymbol { IsIndexer: true };

    void DeclareComponentForwarders(INamedTypeSymbol sym)
    {
        sb.AppendLine();
        GenerateAsMethod(sym);
        DeclareClassImplicitOperator(sym);

        GenerateAllMembers(sym);
    }

    private void GenerateAsMethod(INamedTypeSymbol sym) =>
        sb.AppendLine($"""
                public {sym.GlobalName} As{sym.Name}() => 
                    ({sym.GlobalName})this.Value;
            """);

    private void DeclareClassImplicitOperator(INamedTypeSymbol sym)
    {
        if (sym.TypeKind is not TypeKind.Interface)
        {
            sb.AppendLine($"""
                    public static implicit {sym.GlobalName}({symbol.GlobalName} i) => 
                        i.As{sym.Name}();
                """);

        }
    }

    private void GenerateAllMembers(INamedTypeSymbol sym)
    {
        var fact = new MemberForwarderFactory(sym.Name);
        foreach (var member in sym.GetMembers())
        {
            if (IsNotSpecialInternalMethod(member) &&
                fact.Create(member) is { } forwarder)
                forwarders.Add(forwarder);
        }
    }

    private void OutputForwarders()
    {
        foreach (var forwarder in forwarders.GroupBy(i => i, MemberForwarder.Comparer))
        {
            if (forwarder.Count() == 1)
            {
                forwarder.First().WriteImplicitForwarder(sb);
            }
            else
            {
                foreach (var f2 in forwarder)
                {
                    f2.WriteExplicitForwarder(sb);
                }
            }
        }
    }

}
