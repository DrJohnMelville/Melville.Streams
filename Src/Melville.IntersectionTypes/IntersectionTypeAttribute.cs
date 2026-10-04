using System;
using System.Runtime.CompilerServices;

namespace Melville.IntersectionTypes;

public sealed class IntersectionTypeAttribute : Attribute
{ 
}

public interface IIntersection 
{
    object Value { get; }
}