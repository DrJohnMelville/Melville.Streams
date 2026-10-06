using Melville.IntersectionTypes.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Immutable;
using System.Text;

namespace Melville.IntersectionTypes.CodeGen;

internal readonly struct MemberForwarderFactory(string target)
{
    public MemberForwarder? Create(ISymbol symbol) => symbol switch
    {
        { DeclaredAccessibility: not Accessibility.Public } => null,
        { IsStatic: true } => null,
        IPropertySymbol { IsIndexer: true } ps => CreateIndexer(ps),
        IPropertySymbol ps => CreateProperty(ps, ps.Name, $".{ps.Name}"),
        IEventSymbol es => CreateEvent(es),
        IMethodSymbol ms => CreateMethod(ms),
        _ => null
    };

    private MemberForwarder? CreateIndexer(IPropertySymbol ps)
    {
        StringBuilder parameters = new();
        var (arguments, types) = ProcessParameterList(ps.Parameters, parameters);
        return CreateProperty(ps, $"this[{parameters.ToString()}]", $"[{arguments}]", types);
    }

    private MemberForwarder? CreateProperty(IPropertySymbol ps, string name, string refCall,
                                      string? parameterTypes = null)
    {
        StringBuilder sb = new();
        sb.AppendLine($$"""{{name}} {""");
        var refPrefix = ComputeRefPrefix(ps);

        bool generatedArm = false;

        if (ps.GetMethod is { DeclaredAccessibility: Accessibility.Public } gm)
        {
            sb.AppendLine($"        get => {refPrefix}{target}{refCall};");
            generatedArm = true;
        }
        if (ps.SetMethod is
            {
                DeclaredAccessibility: Accessibility.Public,
                IsInitOnly: false
            } sm)
        {
            sb.AppendLine($"        set => {target}{refCall} = value;");
            generatedArm = true;
        }
        sb.AppendLine("    }");

        return generatedArm ? new(refPrefix + ps.Type.GlobalName, sb.ToString(), ps.Name, parameterTypes, ps) : null;

    }

    private string ComputeRefPrefix(IPropertySymbol ps) => ps.RefKind is RefKind.Ref ? "ref " : "";

    private MemberForwarder CreateMethod(IMethodSymbol ms)
    {
        StringBuilder code = new();

        code.Append($"{ms.Name}(");

        var (arguments, parameterTypes) = ProcessParameterList(ms.Parameters, code);

        code.AppendLine(") =>");
        code.AppendLine($"        {RefArgumentPrefix(ms.RefKind)}{target}.{ms.Name}({arguments});");



        return new MemberForwarder(
            RefParameterPrefix(ms.RefKind) + ms.ReturnType.GlobalName, code.ToString(), ms.Name, 
            parameterTypes, ms);
    }


    private (string Arguments, string ParameterTypes)
        ProcessParameterList(ImmutableArray<IParameterSymbol> parameters, StringBuilder code)
    {
        StringBuilder argumentTypes = new();
        var delimiters = new FirstDifferenceBuffer<string>("", ",");
        StringBuilder items = new();
        foreach (var parameter in parameters)
        {
            var delim = delimiters.Next();
            code.AppendLine(delim);
            code.Append($"        {RefParameterPrefix(parameter.RefKind)}{parameter.Type.GlobalName} {parameter.Name}");

            TryAddDefaultValue(code, parameter);

            items.Append(delim);
            items.Append($"{RefArgumentPrefix(parameter.RefKind)}{parameter.Name}");

            argumentTypes.AppendLine(parameter.Type.GlobalName);
        }

        return (items.ToString(), argumentTypes.ToString());
    }

    private void TryAddDefaultValue(StringBuilder code, IParameterSymbol parameter)
    {
        if (parameter.HasExplicitDefaultValue)
        {
            code.Append($" = {CreateConstant(parameter)}");
        }
    }

    public string CreateConstant(IParameterSymbol value) => value.ExplicitDefaultValue switch
    {
        null => "default",
        string s => SymbolDisplay.FormatLiteral(s, true),
        char c => SymbolDisplay.FormatLiteral(c, true),
        var e when value.Type is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType =>
            $"({enumType.GlobalName}) {e}",
        true => "true",
        false => "false",
        var i => i.ToString()

    };

    private string RefParameterPrefix(RefKind refKind) => refKind switch
    {
        RefKind.None => "",
        RefKind.Out => "out ",
        RefKind.RefReadOnlyParameter => "ref readonly ",
        RefKind.Ref => "ref ",
        RefKind.In => "in ",
        _ => throw new ArgumentException("Unknown ref type")
    };
    private string RefArgumentPrefix(RefKind refKind) => refKind switch
    {
        RefKind.RefReadOnlyParameter => "ref ",
        _ => RefParameterPrefix(refKind)
    };

    MemberForwarder? CreateEvent(IEventSymbol es)
    {
        StringBuilder code = new();

        code.AppendLine($$"""{{es.Name}} {""");
        if (es.AddMethod is { })
        {
            code.AppendLine($"        add => {target}.{es.Name} += value;");
        }
        if (es.RemoveMethod is { })
        {
            code.AppendLine($"        remove => {target}.{es.Name} -= value;");
        }
        code.AppendLine("    }");

        return new($"event {es.Type}", code.ToString(), es.Name, null, es);
    }
}
