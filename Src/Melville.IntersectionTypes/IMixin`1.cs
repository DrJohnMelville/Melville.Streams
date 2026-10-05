namespace Melville.IntersectionTypes;


/// <summary>
/// This is a marker interface.  The code generator will integrate the members in the mixin into
/// the class or struct that inherits from the IMixin class.
/// 
/// thus:
/// public partial class Child: IMixin&lt;MixStruct&gt;
/// {
/// }
/// 
/// public readonly struct MixStruct(Child inner) {
///     public void MethodToForward();
/// }
/// 
/// Note the constructor on the mixin struct which provides context to the mixin.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IMixin<T>
{
    // This mixin identifies they types used to declare a mixin.
}