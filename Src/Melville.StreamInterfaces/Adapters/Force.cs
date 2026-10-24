using System;

namespace Melville.StreamInterfaces.Adapters;

/// <summary>
/// This is a little helper class to add things that may or may not be disposable in a using claues
/// </summary>
public static class Force
{
    /// <summary>
    /// Allows you to use any object in a using clause
    /// </summary>
    /// <param name="source">An object that may or may not be disposable</param>
    /// <returns>The argument if it implement IDisposable, a sham IDisposable otherwise</returns>
    public static IDisposable Using(object? source) => source as IDisposable ?? FakeDisposable;

    /// <summary>
    /// Allows you to use any object in a using clause
    /// </summary>
    /// <param name="source">An object that may or may not be disposable</param>
    /// <param name="reflected">Thye first argument/</param>
    /// <returns>The argument if it implement IDisposable, a sham IDisposable otherwise</returns>
    public static IDisposable Using<T>(T source, out T reflected)
    {
        reflected = source;
        return Using(source);
    }

    private static IDisposable FakeDisposable = new FakeDisposeImpl();
    private class FakeDisposeImpl : IDisposable
    {
        public void Dispose() { }
    }
}