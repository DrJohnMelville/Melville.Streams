using System;

namespace Melville.StreamInterfaces.ParsingBuffers;

/// <summary>
/// Factory methods for creating a span parsing buffer.
/// </summary>
public static class SpanParsingBufferFactory
{
    /// <summary>
    /// Create a Span parsing buffer from a block of memory.
    /// </summary>
    /// <param name="src">T#he data to parse</param>
    public static SpanParsingBuffer AsSpanParsingBuffer(this ReadOnlySpan<byte> src) =>
        new SpanParsingBuffer(src);

    /// <summary>
    /// Create a Span parsing buffer from a block of memory.
    /// </summary>
    /// <param name="src">T#he data to parse</param>
    public static SpanParsingBuffer AsSpanParsingBuffer(this ReadOnlyMemory<byte> src) =>
        new SpanParsingBuffer(src.Span);

    /// <summary>
    /// Create a Span parsing buffer from a block of memory.
    /// </summary>
    /// <param name="src">T#he data to parse</param>
    public static SpanParsingBuffer AsSpanParsingBuffer(this Memory<byte> src) =>
        new SpanParsingBuffer(src.Span);
}