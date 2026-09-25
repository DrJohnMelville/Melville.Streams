using Microsoft.CodeAnalysis;
using System;

namespace Melville.IntersectionTypes.MixinGenerator;

public readonly struct MixinCodeGenerator(
    INamedTypeSymbol parent,
    INamedTypeSymbol mixin,
    SourceProductionContext context)
{
    public void Emit() {
        ;
    }
}