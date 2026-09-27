using Microsoft.CodeAnalysis;
using System;

namespace Melville.IntersectionTypes.CodeGen;

public static class SymbolOperations
{
    extension(ISymbol? sym)
    {
        public string GlobalName => sym?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ??
           throw new InvalidOperationException("Cannot find symbol name");
        public string CSharpName => sym?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ??
           throw new InvalidOperationException("Cannot find symbol name");
    }

    extension(ITypeSymbol typeSymbol)
    {

        public string NullableGlobalName()
        {
            var ret = typeSymbol.GlobalName;

            return typeSymbol.IsNullable || ret.EndsWith("?") ? ret : $"{ret}?";
        }

        private bool IsNullable =>
            typeSymbol.NullableAnnotation == NullableAnnotation.Annotated ||
                            typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

    }
}
