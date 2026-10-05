using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces;

/// <summary>
/// Implements copying IReaders to IWriters/
/// </summary>
public static class CopyToImplementations
{
    extension (ISyncReader self)
    {
        /// <summary>
        /// Copy this reader to the giver writer, writing synchronously.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        public void CopyTo(ISyncWriter writer) => self.CopyTo(writer, self.DesiredCopyBufferSize());

        /// <summary>
        /// Copy this reader to the giver writer, writing synchronously.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="desiredBuffer">The buffer size to use in the copying.</param>
        public void CopyTo(ISyncWriter writer, int desiredBuffer)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(desiredBuffer);
            try
            {
                while (true)
                {
                    var read = self.Read(buffer);
                    if (read is 0) return;
                    writer.Write(buffer, 0, read);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

    extension (IAsyncReader self)
    {
        /// <summary>
        /// Copy this reader to a writer, writing asynchronously
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="cancel">A cancellation token</param>
        public ValueTask CopyToAsync(IAsyncWriter writer, CancellationToken cancel = default) =>
            self.CopyToAsync(writer, self.DesiredCopyBufferSize(), cancel);

        /// <summary>
        /// Copy this reader to a writer, writing asynchronously
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="bufferSize">Size of buffer to use in the copy operation./</param>
        /// <param name="cancel">A cancellation token</param>
        public async ValueTask CopyToAsync(
            IAsyncWriter writer, int bufferSize, CancellationToken cancel = default)
        {
            var readBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            var writeBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                int lastRead = 0;
                do
                {
                    if (cancel.IsCancellationRequested) return;

                    var readTask = self.ReadAsync(readBuffer);
                    if (lastRead > 0) await writer.WriteAsync(writeBuffer, 0, lastRead);
                    lastRead = await readTask;
                    Swap(ref readBuffer, ref writeBuffer);
                } while (lastRead > 0);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(readBuffer);
                ArrayPool<byte>.Shared.Return(writeBuffer);
            }
        }

    }
    private static void Swap<T>(ref T a, ref T b) => (a, b) = (b, a);

}

internal static class CopyHelpers
{
    // This value was originally picked to be the largest multiple of 4096 that is still smaller than the large object heap threshold (85K).
    // The CopyTo{Async} buffer is short-lived and is likely to be collected at Gen0, and it offers a significant improvement in Copy
    // performance.  Since then, the base implementations of CopyTo{Async} have been updated to use ArrayPool, which will end up rounding
    // this size up to the next power of two (131,072), which will by default be on the large object heap.  However, most of the time
    // the buffer should be pooled, the LOH threshold is now configurable and thus may be different than 85K, and there are measurable
    // benefits to using the larger buffer size.  So, for now, this value remains.
    const long DefaultCopyBufferSize = 81920;

    extension(IStream self)
    {

        public int DesiredCopyBufferSize() =>
            (int)long.Clamp(self.BytesLeft(), 16, DefaultCopyBufferSize);

        public long BytesLeft() => self switch
        {
            IStreamLength sl and IStreamPosition pos => sl.Length - pos.Position,
            IStreamLength sl => sl.Length,
            _ => long.MaxValue
        };
    }

    extension(ArgumentException exec)
    {
        public static void ThrowIfInvalidBufferSize(
            int value, int inclusiveMax, [CallerArgumentExpression(nameof(value))] string caller = "")
        {
            if ((uint)value > inclusiveMax)
                throw new ArgumentException(
                    $"""
                    "{caller}" should be between 0 and {inclusiveMax} but is {value}.
                    """
                    );
        }
    }
}
