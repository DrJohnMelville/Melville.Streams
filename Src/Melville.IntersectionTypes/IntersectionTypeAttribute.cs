using Microsoft.CodeAnalysis;
using System;

namespace Melville.IntersectionTypes;

public sealed class IntersectionTypeAttribute : Attribute
{ 
}

public interface IIntersectionClass<T>
{
    // empty marker interface used so we can designate classes in the parent list without
    // actuallly inheriting from them.
}
