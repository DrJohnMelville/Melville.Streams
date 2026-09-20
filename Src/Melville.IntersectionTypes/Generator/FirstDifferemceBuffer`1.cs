using System;
using System.Linq;

namespace Melville.IntersectionTypes.Generator;

public ref struct FirstDifferenceBuffer<T>(T first, T subsequent)
{
    private T next = first;
    public T Next()
    {
        var ret = next;
        next = subsequent;
        return ret;
    }
}