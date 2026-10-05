namespace Melville.IntersectionTypes.Generator;

internal ref struct FirstDifferenceBuffer<T>(T first, T subsequent)
{
    private T next = first;
    public T Next()
    {
        var ret = next;
        next = subsequent;
        return ret;
    }
}