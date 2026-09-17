using System;
using System.Linq;

namespace Melville.IntersectionTypes;

public ref struct FirstDifferemceBuffer<T>(T first, T subsequent)
{
    private T next = first;
    public T Next()
    {
        var ret = next;
        next = subsequent;
        return ret;
    }
}