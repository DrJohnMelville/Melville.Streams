using Melville.INPC;
using System;
using System.Buffers.Binary;

namespace Melville.StreamInterfaces.ParsingBuffers;

/// <summary>
/// Tools for sequentially reading from a ReadOnlySpan of bytes.
/// </summary>
/// <param name="span">The source of data for the buffer</param>
public ref partial struct SpanParsingBuffer(ReadOnlySpan<byte> span)
{
    private ReadOnlySpan<byte> span = span;

    /// <summary>
    /// Peek at the unread buffer
    /// </summary>
    public ReadOnlySpan<byte> Peek() => span;

    /// <summary>
    /// Peek at the unread buffer up to a given number of bytes
    /// </summary>
    /// <param name="bytes">Maximum number of bytes to return</param>
    public ReadOnlySpan<byte> Peek(int bytes) => span[..Math.Min(bytes, span.Length)];

    /// <summary>
    /// The number of bytes remaining in the buffer
    /// </summary>
    public int RemainingLength => span.Length;

    /// <summary>
    /// Counter that is advanced when the buffer is consumed.
    /// </summary>
    public long RelativePosition { get; set; }

    /// <summary>
    /// Consume a number of bytes from the front of the buffer.
    /// </summary>
    /// <param name="delta">The number of bytes to consume.</param>
    public void Advance(int delta)
    {
        switch (delta)
        {
            case < 0:
                throw new InvalidOperationException("Cannot advance backward.");
            case 0: return;
        }
        RelativePosition += delta;
        span = span[delta..];
    }

    /// <summary>
    /// Skip forward to the given relative position.
    /// </summary>
    /// <param name="newRelativePosition">The desired next relative position.</param>
    /// <exception cref="InvalidOperationException">The desired relative position is less than the current position
    /// </exception>
    public void AdvanceToRelativePosition(long newRelativePosition) => 
        Advance((int)(newRelativePosition - RelativePosition));


    [MacroItem(16, 2)]
    [MacroItem(32, 4)]
    [MacroItem(64, 8)]
    [MacroItem(128, 16)]
    [MacroCode("""
    /// <summary>
    /// Get a big endian UInt ~0~ out of the buffer,
    /// </summary>
    public UInt~0~ GetUInt~0~BigEndian()
    {
        var ret = BinaryPrimitives.ReadUInt~0~BigEndian(span);
        Advance(~1~);
        return ret;
    }
    /// <summary>
    /// Get a big endian UInt ~0~ out of the buffer,
    /// </summary>
    public UInt~0~ GetUInt~0~LittleEndian()
    {
        var ret = BinaryPrimitives.ReadUInt~0~LittleEndian(span);
        Advance(~1~);
        return ret;
    }
    /// <summary>
    /// Get a big endian Int ~0~ out of the buffer,
    /// </summary>
    public Int~0~ GetInt~0~BigEndian()
    {
        var ret = BinaryPrimitives.ReadInt~0~BigEndian(span);
        Advance(~1~);
        return ret;
    }
    /// <summary>
    /// Get a big endian Int ~0~ out of the buffer,
    /// </summary>
    public Int~0~ GetInt~0~LittleEndian()
    {
        var ret = BinaryPrimitives.ReadInt~0~LittleEndian(span);
        Advance(~1~);
        return ret;
    }
    """)]
    partial void FooMethod();

    /// <summary>
    /// Get a byte out of the buffer,
    /// </summary>
    public byte GetUInt8()
    {
        var ret = span[0];
        Advance(1);
        return ret;
    }
    /// <summary>
    /// Get a signed byte out of the buffer,
    /// </summary>
    public sbyte GetInt8()
    {
        var ret = (sbyte)span[0];
        Advance(1);
        return ret;
    }
}
