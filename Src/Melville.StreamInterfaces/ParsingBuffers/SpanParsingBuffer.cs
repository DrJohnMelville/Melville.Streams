using Melville.INPC;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography.X509Certificates;

namespace Melville.StreamInterfaces.ParsingBuffers;



/// <summary>
/// Tools for sequentially reading from a ReadOnlySpan of bytes.
/// </summary>
/// <param name="span">The source of data for the buffer</param>
public ref partial struct SpanParsingBuffer(ReadOnlySpan<byte> span)
{
    private ReadOnlySpan<byte> span = span;

    #region Buffer access and status
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

    #endregion

    #region Advance Methods

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
    #endregion

    #region Integer Parsing Methods

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

    public ulong GetUIntBigEndian(int bytes)
    {
        ulong ret = 0;
        for (int i = 0; i < bytes; i++)
        {
            ret <<= 8;
            ret |= this.GetUInt8();
        }
        return ret;
    }
    #endregion

    #region Try get and peek methods
    /// <summary>
    /// Try to get a byte out of the buffer, and advance if successful.
    /// </summary>
    /// <param name="value">The byte if successful, zero otherwise.</param>
    /// <returns>True if a byte is present, false otherwise.</returns>
    public bool TryGetUInt8(out byte value)
    {
        if (TryPeekUInt8(out value))
        {
            Advance(1);
            return true;
        }
        return false;
    }
    /// <summary>
    /// Try to get a byte out of the buffer, and advance if successful.
    /// </summary>
    /// <param name="value">The byte if successful, zero otherwise.</param>
    /// <returns>True if a byte is present, false otherwise.</returns>
    public bool TryPeekUInt8(out byte value)
    {
        if (RemainingLength < 1)
        {
            value = 0;
            return false;
        }
        value = Peek()[0];
        return true;
    }

    #endregion

    #region SearchMethods 
    /// <summary>
    /// Advance to and consume the next index that complies with the accept parameter 
    /// </summary>
    /// <param name="accept">A search values of items to accept.</param>
    /// <param name="value">the found Character</param>
    /// <returns>true if compliant character is found, false otherwise</returns>
    public bool TryGetNext(SearchValues<byte> accept, out byte value)
    {
        if (TryAdvanceTo(accept))
        {
            value = GetUInt8();
            return true;
        }
        else
        {
            value = 0;
            return false;
        }
    }
    /// <summary>
    /// Advance to and consume the next index that does not match the exclude parameter 
    /// </summary>
    /// <param name="exclude">A search values of items to exclude.</param>
    /// <param name="value">the found Character</param>
    /// <returns>true if compliant character is found, false otherwise</returns>
    public bool TryGetNextExcept(SearchValues<byte> exclude, out byte value)
    {

        if (TryAdvanceToExcept(exclude))
        {
            value = GetUInt8();
            return true;
        }
        else
        {
            value = 0;
            return false;
        }
    }

    /// <summary>
    /// Attempt to the next item matching the accept parameter.  Does not move if no item found
    /// </summary>
    /// <param name="accept">SearchVales describing items to accept</param>
    /// <returns>True if the advance was successful false if not.</returns>
    public bool TryAdvanceTo(SearchValues<byte> accept)
    {
        var index = Peek().IndexOfAny(accept);
        if (index < 0) return false;
        Advance(index);
        return true;
    }
    /// <summary>
    /// Attempt to the next item matching the accept parameter.  Does not move if no item found
    /// </summary>
    /// <param name="accept">SearchVales describing items to accept</param>
    /// <returns>True if the advance was successful false if not.</returns>
    public bool TryAdvanceToExcept(SearchValues<byte> exclude)
    {
        var index = Peek().IndexOfAnyExcept(exclude);
        if (index < 0) return false;
        Advance(index);
        return true;
    }

    #endregion
}
