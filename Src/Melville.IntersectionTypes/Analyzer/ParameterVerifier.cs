using Melville.IntersectionTypes.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace Melville.IntersectionTypes.Analyzer;

internal readonly struct ParameterVerifier(
    ITypeSymbol argumentType,
    ITypeSymbol desiredType,
    Compilation compilation)
{
    public ITypeSymbol? Check()
    {
        foreach (var thisInterface in DesiredTypes())
        {
            if (!ArgumentMatchesType(thisInterface)) return thisInterface;
        }
        return null;
    }

    private bool ArgumentMatchesType(ITypeSymbol? desiredInterface)
    {
        if (desiredInterface is null) return true;
        if (compilation.HasImplicitConversion(argumentType, desiredInterface))
            return true ;
        foreach (var parent in argumentType.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(desiredInterface, parent)) return true;
        }
        return false;
    }

    private ITypeSymbol? GetSentinelType() =>
    compilation.GetTypeByMetadataName(typeof(IntersectionTypeAttribute).FullName);


    private IEnumerable<ITypeSymbol> DesiredTypes()
    {
        if (desiredType is not null &&
            GetSentinelType() is { } sentinelType)
        {
            foreach (var attr in desiredType.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, sentinelType))
                    return desiredType.GetComponentTypes(compilation);
            }
        }
        return [];
    }

}
