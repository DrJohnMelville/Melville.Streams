using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Text;

namespace Melville.IntersectionTypes.CodeGen;

internal readonly struct MethodForwardFacade
{
    public readonly INamedTypeSymbol source;
    public readonly StringBuilder sb;
    public readonly HashSet<MemberForwarder> hostMethods;
    private readonly string prefix;
    public MethodForwardFacade(INamedTypeSymbol source, INamedTypeSymbol host,
        string prefix, StringBuilder sb)
    {
        this.source = source;
        this.sb = sb;
        this.prefix = prefix;
        hostMethods = new(GetHostMethods(host), MemberForwarder.Comparer);
    }

    public void WriteMethods()
    {
        foreach (var item in GetHostMethods(source))
        {
            if (hostMethods.Contains(item)) continue;
            item.WriteImplicitForwarder(sb);
        }
    }

    private IEnumerable<MemberForwarder> GetHostMethods(INamedTypeSymbol methodSource)
    {
        var fact = new MemberForwarderFactory(prefix);
        foreach (var member in methodSource.GetMembers())
        {
            if (IsNotSpecialInternalMethod(member) &&
                fact.Create(member) is { } forwarder)
                yield return forwarder;
        }

    }

    // we don't generate the component methods of properties, events, or indexers, because
    // we generate those using higher level constructs
    private bool IsNotSpecialInternalMethod(ISymbol i) => i.CanBeReferencedByName ||
        i is IPropertySymbol { IsIndexer: true };

}