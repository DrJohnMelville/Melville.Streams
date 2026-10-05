using System;
using System.ComponentModel;
using System.Reflection.Metadata;

namespace Melville.IntersectionTypes;

/// <summary>
/// This is a internal type that implements type checking for the intersection types
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TypeVerifier
{
    /// <summary>
    /// Verify the item parameter implements all the correct types.
    /// </summary>
    /// <param name="item">Instance to test.</param>
    /// <param name="types">Types to test.</param>
    /// <param name="result">A final value, with all other IIntersection wrappers stripped off</param>
    /// <returns>True if the item implements all the constraints, false otherwise.</returns>
    public static bool TryVerifyType(object item, ReadOnlySpan<Type> types, 
       out object result)
    {
        if (item is IIntersection intersection)
            return TryVerifyType(intersection.Value, types, out result);

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

    /// <summary>
    /// Verify an item implements a set of types, and throw if it does not.
    /// </summary>
    /// <param name="item">Instance to test.</param>
    /// <param name="types">Types to verify.</param>
    /// <returns>The object with all the intersection wrappers removed.</returns>
    /// <exception cref="ArgumentException">If the item does not implement all the types.</exception>
    public static object Verify(object item, ReadOnlySpan<Type> types)
    {
        if (TryVerifyType(item, types, out var ret)) return ret!;
        throw new ArgumentException($"{item} does not implement {ret}.");
    }
}