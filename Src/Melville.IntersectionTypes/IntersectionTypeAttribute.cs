using System;

namespace Melville.IntersectionTypes;

public sealed class IntersectionTypeAttribute : Attribute
{
    public Type[] Members { get; set; }
    public IntersectionTypeAttribute(params Type[] members)
    {
        Members = members;
    }
}
