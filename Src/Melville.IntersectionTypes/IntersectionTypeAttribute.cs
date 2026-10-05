using System;
using System.Runtime.CompilerServices;

namespace Melville.IntersectionTypes;

/// <summary>
/// This attribute marks an intersection type.
/// 
/// The model is a readonly struct:
/// 
/// public readonly partial struct Name: IA, IB, IC, IIntersectionClass<ClassType>{
/// {
/// }
/// 
/// the code generator will generate the remainder of the class.
/// </ClassType>
/// </summary>
public sealed class IntersectionTypeAttribute : Attribute
{ 
}

/// <summary>
/// This is an interface that marks an intersection type.  The code generator adds this interface and uses it to
/// implement intersection to intersection conversions without wrapping in boxed structs.
/// </summary>
public interface IIntersection 
{
    /// <summary>
    /// This is the inner reference known to iumplement all the interfaces and/or class in the declaration.
    /// </summary>
    object Value { get; }
}