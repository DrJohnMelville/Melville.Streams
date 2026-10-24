using System;

namespace Melville.StreamInterfaces.ParsingBuffers;

internal class DisposeNode(IDisposable oldDep, IDisposable disposable) : IDisposable
{
    public void Dispose()
    {
        disposable.Dispose();
        oldDep.Dispose();
    }

    public static IDisposable? Create(IDisposable? oldObj, IDisposable? newObj) => (oldObj, newObj) switch
    {
        (_, null) => oldObj,
        (null,_) => newObj,
        _ => new DisposeNode(oldObj, newObj)
    };
}
