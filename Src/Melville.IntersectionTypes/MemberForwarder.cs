using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Melville.IntersectionTypes;

public class MemberForwarder(
    string returnType, 
    string body, 
    string name,
    string parameterTypes)
{
    public void WriteImplicitForwarder(StringBuilder sb)
    {
        sb.AppendLine($"    public {returnType} {body}");
    }
}

internal readonly struct MemberForwarderFactory(string targetName)
{
    public MemberForwarder? Create(ISymbol symbol) => symbol switch
    {
        { DeclaredAccessibility : not Accessibility.Public} => null,
        { IsStatic: true} => null,
        IPropertySymbol { IsIndexer: true } ps => CreateIndexer(ps),
        IPropertySymbol ps => CreateProperty(ps, ps.Name, $".{ps.Name}"),
        IEventSymbol es => CreateEvent(es),
        IMethodSymbol ms => CreateMethod(ms),
        _ => null
    };

    private MemberForwarder? CreateIndexer(IPropertySymbol ps)
    {
        StringBuilder parameters = new();
        var arguments = ProcessParameterList(ps.Parameters, parameters); 
        return CreateProperty(ps, $"this[{parameters.ToString()}]", $"[{arguments}]");
    }

    private MemberForwarder? CreateProperty (IPropertySymbol ps, string name, string refCall)
    {
        StringBuilder sb = new();
        sb.AppendLine($$"""{{name}} {""");
        var tgm = ps.GetMethod;

        bool generatedArm = false;

        if (ps.GetMethod is { DeclaredAccessibility: Accessibility.Public } gm)
        {
            sb.AppendLine($"        get => As{targetName}(){refCall};");
            generatedArm = true;
        }
        if (ps.SetMethod is { DeclaredAccessibility: Accessibility.Public,
                              IsInitOnly: false} sm)
        {
            sb.AppendLine($"        set => As{targetName}(){refCall} = value;");
            generatedArm = true;
        }
        sb.AppendLine("    }");

        return generatedArm?new(ps.Type.GlobalName, sb.ToString(), ps.Name, ""): null;

    }

    private MemberForwarder CreateMethod(IMethodSymbol ms)
    {
        StringBuilder code = new();

        code.Append($"{ms.Name}(");

        var items = ProcessParameterList(ms.Parameters, code);

        code.AppendLine(") =>");
        code.AppendLine($"        {RefArgumentPrefix(ms.RefKind)}As{targetName}().{ms.Name}({items});");

        

        return new MemberForwarder(RefParameterPrefix(ms.RefKind) + ms.ReturnType.GlobalName, code.ToString(), ms.Name, items);
    }


    private string ProcessParameterList(ImmutableArray<IParameterSymbol> parameters, StringBuilder code)
    {
        StringBuilder arguments = new();
        var delimiters = new FirstDifferemceBuffer<string>("", ",");
        StringBuilder items = new();
        foreach (var parameter in parameters)
        {
            var delim = delimiters.Next();
            code.AppendLine(delim);
            code.Append($"        {RefParameterPrefix(parameter.RefKind)}{parameter.Type.GlobalName} {parameter.Name}");

            items.Append(delim);
            items.Append($"{RefArgumentPrefix(parameter.RefKind)}{parameter.Name}");

            arguments.AppendLine(parameter.Type.GlobalName);
        }

        return items.ToString();
    }
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
           code.AppendLine($"        add => As{targetName}().{es.Name} += value;");
        }
        if (es.RemoveMethod is { })
        {
            code.AppendLine($"        remove => As{targetName}().{es.Name} -= value;");
        }
        code.AppendLine("    }");

        return new($"event {es.Type}", code.ToString(), es.Name, "");
    }
}

public ref struct FirstDifferemceBuffer<T> (T first, T subsequent)
{
    private T next = first;
    public T Next()
    {
        var ret = next;
        next = subsequent;
        return ret;
    }
}