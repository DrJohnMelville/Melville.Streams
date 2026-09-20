using System;

namespace Melville.IntersectionTypes;

public static class TypeVerifier
{
    public static void VerifyTypes(object item, params ReadOnlySpan<Type> types)
    {
        if (InvalidType(item, types) is { } typeName)
         throw new ArgumentException($"{item} does not implement {typeName}.");
    }

    public static bool IsValidType(object item, params ReadOnlySpan<Type> types) =>
        InvalidType(item, types) is null;
    private static string? InvalidType (object item, params ReadOnlySpan<Type> types)
    {
        foreach (var type in types)
        {
            if (!type.IsInstanceOfType(item))
                return type.Name;
        }
        return null;
    }
}