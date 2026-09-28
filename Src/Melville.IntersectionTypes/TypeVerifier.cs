using System;
using System.Reflection.Metadata;

namespace Melville.IntersectionTypes;

public static class TypeVerifier
{
    public static bool TryVerifyType(object item, ReadOnlySpan<Type> types, 
       out object result)
    {
        foreach (var type in types)
        {
            if (!type.IsInstanceOfType(item))
            {
                result = type.Name;
                return false;
            }
        }
        result = item;
        return true;
    }
    public static object Verify(object item, ReadOnlySpan<Type> types)
    {
        if (TryVerifyType(item, types, out var ret)) return ret!;
        throw new ArgumentException($"{item} does not implement {ret}.");
    }
}