namespace Melville.IntersectionTypes;

/// <summary>
/// This is an marker intersection.  This is used in the parent list of an intersection type
/// to allow a class to be a member of an intersection type without the struct actually inheriting
/// from the class or struct.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IIntersectionClass<T>
{
    // empty marker interface used so we can designate classes in the parent list without
    // actuallly inheriting from them.
}
