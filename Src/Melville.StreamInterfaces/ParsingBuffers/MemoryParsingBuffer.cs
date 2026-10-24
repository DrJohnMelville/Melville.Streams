using Melville.INPC;
using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Melville.StreamInterfaces.ParsingBuffers;

/// <summary>
/// Tools for sequentially reading from a ReadOnlySpan of bytes.
/// </summary>
/// <param name="memory">The source of data for the buffer</param>
public partial class MemoryParsingBuffer(ReadOnlyMemory<byte> memory)
{
    ReadOnlyMemory<byte> memory = memory;
    ReadOnlySpan<byte> span => memory.Span;


    private void InnerAdvance(int delta) => memory = memory[delta..];

    /// <summary>
    /// Peek at the unread buffer
    /// </summary>
    public ReadOnlySpan<byte> Peek() => span;

    [MacroItem(typeof(SearchValues), typeof(BinaryPrimitives))]
    [MacroCode(SpanParsingMacros.SingleShotCode)]
    partial void SingleShotHolder();

    [MacroItem(16, 2)]
    [MacroItem(32, 4)]
    [MacroItem(64, 8)]
    [MacroItem(128, 16)]
    [MacroCode(SpanParsingMacros.NumberMethods)]
    partial void NumberMethods();
}

