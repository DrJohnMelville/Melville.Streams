using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces.Adapters;

internal class SyncFromAsyncReader(IAsyncReader source) : ISyncReader

{
    public int Read(Span<byte> buffer) => ReadSyncFromAsyncMethods.Read(source, buffer);

    public int Read(byte[] buffer, int position, int length) =>
        ReadSyncFromAsyncMethods.Read(source, buffer, position, length);

}

/// <summary>
/// This class defines extensions used to wrap an IAsyncReader with an ISyncReader
/// </summary>
public static class ReadSyncFromAsyncMethods
{
    internal static int Read(IAsyncReader source,Span<byte> buffer)
    {
        // this is the bad one -- we have to get a buffer because we cannot send the span into an async op.
        var len = buffer.Length;
        var buf = ArrayPool<byte>.Shared.Rent(len);
        var ret = Task.Run<int>(() => source.ReadAsync(buf[..len], default).AsTask()).GetAwaiter().GetResult();
        buf[..ret].CopyTo(buffer);
        ArrayPool<byte>.Shared.Return(buf);
        return ret;
    }

    internal static int Read(IAsyncReader source, byte[] buffer, int position, int length) =>
        Task.Run<int>(
            () => source.ReadAsync(buffer, position, buffer.Length, default)
            .AsTask()).GetAwaiter().GetResult();

    /// <summary>
    /// Wrap an IAsyncReader with an ISyncreader. 
    /// </summary>
    /// <param name="reader"></param>
    /// <returns>The argument if it implements ISyncReader, otherwise an adaptor that uses the threadpool</returns>
    public static ISyncReader AsISyncReader(this IAsyncReader reader) =>
        reader is ISyncReader sync ? sync : new SyncFromAsyncReader(reader);
}