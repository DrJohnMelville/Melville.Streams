using System;

namespace Melville.IntersectionTypes;

public static class TypeVerifier
{
    public static void VerifyTypes(object item, params ReadOnlySpan<Type> types)
    {
        foreach (var type in types)
        {
            if (!type.IsInstanceOfType(item))
                throw new ArgumentException($"{item} does not implement {type.Name}.");
        }
    }
}