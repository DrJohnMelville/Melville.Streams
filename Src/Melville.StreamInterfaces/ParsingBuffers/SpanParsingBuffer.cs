using Melville.INPC;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Markup;

namespace Melville.StreamInterfaces.ParsingBuffers;

/// <summary>
/// Tools for sequentially reading from a ReadOnlySpan of bytes.
/// </summary>
/// <param name="span">The source of data for the buffer</param>
[StructLayout(LayoutKind.Auto)]
public ref partial struct SpanParsingBuffer(ReadOnlySpan<byte> span)
{
    private ReadOnlySpan<byte> span = span;


    private void InnerAdvance(int delta) => span = span[delta..];

    /// <summary>
    /// Peek at the unread buffer
    /// </summary>
    public ReadOnlySpan<byte> Peek() => span;

    [MacroItem("Foo")]
    [MacroCode(SpanParsingMacros.SingleShotCode)]
    partial void SingleShotHolder();

    [MacroItem(16, 2)]
    [MacroItem(32, 4)]
    [MacroItem(64, 8)]
    [MacroItem(128, 16)]
    [MacroCode(SpanParsingMacros.NumberMethods)]
    partial void NumberMethods();
}

