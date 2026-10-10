using System;
using System.Buffers;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces.Adapters;

internal class SyncFromAsyncWriter(IAsyncWriter source) : ISyncWriter

{
    public void Write(ReadOnlySpan<byte> buffer) => WriteSyncFromAsyncMethods.Write(source, buffer);

    public void Write(byte[] buffer, int position, int length) =>
        WriteSyncFromAsyncMethods.Write(source, buffer, position, length);

}

/// <summary>
/// This class defines extensions used to wrap an IAsyncWriter with an ISyncWriter
/// </summary>
public static class WriteSyncFromAsyncMethods
{
    internal static void Write(IAsyncWriter source, ReadOnlySpan<byte> buffer)
    {
        var buf = ArrayPool<byte>.Shared.Rent(buffer.Length);
        var len = buffer.Length;
        buffer.CopyTo(buf);
        Task.Run(() => source.WriteAsync(buf.AsMemory(0, len))).GetAwaiter().GetResult();
        ArrayPool<byte>.Shared.Return(buf);
    }

    internal static void Write(IAsyncWriter source, byte[] buffer, int position, int length) =>
        Task.Run(
            () => source.WriteAsync(buffer, position, buffer.Length, default)
            .AsTask()).GetAwaiter().GetResult();

    /// <summary>
    /// Wrap an IAsyncWriter with an ISyncWriter. 
    /// </summary>
    /// <param name="Writer"></param>
    /// <returns>The argument if it implements ISyncWriter, otherwise an adaptor that uses the threadpool</returns>
    public static ISyncWriter AsISyncWriter(this IAsyncWriter Writer) =>
        Writer is ISyncWriter sync ? sync : new SyncFromAsyncWriter(Writer);
}