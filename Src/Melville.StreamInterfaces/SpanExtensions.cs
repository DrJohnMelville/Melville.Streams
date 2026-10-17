using Melville.INPC;
using System;
using System.Collections.Generic;
using System.Text;

namespace Melville.StreamInterfaces;


/// <summary>
/// Helper methods on Spans and Memories
/// </summary>
[MacroItem("Span")]
[MacroItem("ReadOnlySpan")]
[MacroItem("Memory")]
[MacroItem("ReadOnlyMemory")]
[MacroCode("""
    /// <summary>
    /// Returns up to the first "max" bytes of the input, less if input's length
    /// is less than mmax
    /// </summary>
    /// <typeparam name="T">The component type of the input</typeparam>
    /// <param name="input">A span or memory to take values from</param>
    /// <param name="max">The maximum number of values to take.</param>
    /// <param name="length">The length of the returned ~0~.</param>
        public static ~0~<T> TakeMax<T>(this ~0~<T> input, int max, out int length) 
    {
        if (input.Length <= max) 
        {
            length = input.Length;
            return input;
        }
        length = max;
        return input[..max];
    }
    /// <summary>
    /// Returns up to the first "max" bytes of the input, less if input's length
    /// is less than mmax
    /// </summary>
    /// <typeparam name="T">The component type of the input</typeparam>
    /// <param name="input">A span or memory to take values from</param>
    /// <param name="max">The maximum number of values to take.</param>
        public static ~0~<T> TakeMax<T>(this ~0~<T> input, int max) =>
            input.TakeMax(max, out var _);
    
    """)]
public static partial class SpanExtensions
{
}
