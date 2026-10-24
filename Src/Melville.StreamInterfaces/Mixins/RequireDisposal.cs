using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;

namespace Melville.StreamInterfaces.Mixins;

/// <summary>
/// This is a helper class that can be called in a destructor to throw only in 
/// </summary>
public static class RequireDisposal
{
    /// <summary>
    /// throw an exception if the object is not disposed.
    /// </summary>
    public static bool ThrowOnMissedDispose { get; set; }

    /// <summary>
    /// Record where each object was created for better finalization.`
    /// </summary>
    public static bool RecordDisposableCreation{
        get => creations is not null;
        set => creations = value ? new ConcurrentDictionary<int, StackTrace>():null; 
    }

    private static ConcurrentDictionary<int, StackTrace>? creations; 

    /// <summary>
    /// Report that an object was created and record the stack trace1
    /// </summary>
    /// <param name="item">The item that was created</param>
    public static void ReportCreation(object item) => creations?.TryAdd(RuntimeHelpers.GetHashCode(item), new StackTrace());

    /// <summary>
    /// Report that an object was disposed
    /// </summary>
    /// <param name="item">The item that was created</param>
    public static void ReportDisposal(object item) => creations?.Remove(RuntimeHelpers.GetHashCode(item), out var _);

    /// <summary>
    /// Report that a dispose got missed -- this is called from the finalizer
    /// </summary>
    /// <param name="item">The item that was not disposed</param>
    /// <exception cref="InvalidOperationException">Always throws -- that is what this does.</exception>
    public static void ReportMissedDispose(IDisposable item)
    {
        if (ThrowOnMissedDispose)
            throw new InvalidOperationException($"Object not disposed. {item}.\r\n{GetCreation(item)}");

        // otherwise
        item.Dispose();
    }
    static string GetCreation(object item)
    {
        return (creations?.TryGetValue(RuntimeHelpers.GetHashCode(item), out var trace) ?? false) ?
            trace.ToString() : "Object creation not recorded.";
    }
}
