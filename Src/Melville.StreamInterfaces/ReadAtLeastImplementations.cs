using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces;

/// <summary>
/// This class holds the ReadAtLeast implementations for the ISyncReader and the IAsyncReader
/// </summary>
public static class ReadAtLeastImplementations
{
    extension(ISyncReader self)
    {

        /// <summary>
        /// Read at least a given number of bytes, maybe more, into a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read bytes into</param>
        /// <param name="minimumBytes">The minimum number of bytes to read.</param>
        /// <param name="throwEndOfStream">Throw an exception if not enought bytes in the stream.</param>
        /// <returns>The number of bytes read.</returns>
        /// <exception cref="EndOfStreamException">If the end of the stream is reached before enough 
        /// bytes are read.</exception>
        public int ReadAtLeast(Span<byte> buffer, int minimumBytes, bool throwEndOfStream = true)
        {
            ArgumentException.ThrowIfInvalidBufferSize(minimumBytes, buffer.Length);
            var total = 0;
            int local;
            do
            {
                local = self.Read(buffer);
                total += local;
                buffer = buffer[local..];
            } while (local > 0 && total < minimumBytes);
            if (throwEndOfStream && total < minimumBytes)
                throw new EndOfStreamException();
            return total;
        }

        /// <summary>
        /// Completely fill the buffer from a stream.
        /// </summary>
        /// <param name="buffer">The buffer to fill.</param>
        /// <param name="throwEndOfStream">If true, will throw if there is not enoug data
        /// to fill the buffer</param>
        /// <returns>The number of bytes read.</returns>
        public int ReadExact(Span<byte> buffer, bool throwEndOfStream = true) =>
            self.ReadAtLeast(buffer, buffer.Length, throwEndOfStream);
    }

    extension(IAsyncReader self)
    {
        /// <summary>
        /// Read at least a given number of bytes from a stream, asynchronously.
        /// </summary>
        /// <param name="buffer">Buffer to read bytes into</param>
        /// <param name="minimumBytes">Minimum number of bytes desired.</param>
        /// <param name="cancel">Cancellation token</param>
        /// <param name="throwEndOfStream">Throw an exception if not enough bytes in the
        /// stream to fulfill the request.</param>
        /// <returns>The number of bytes read.</returns>
        /// <exception cref="EndOfStreamException"></exception>
        public async ValueTask<int> ReadAtLeastAsync(
         Memory<byte> buffer, int minimumBytes, CancellationToken cancel = default, 
         bool throwEndOfStream = true)
        {
            ArgumentException.ThrowIfInvalidBufferSize(minimumBytes, buffer.Length);
            var total = 0;
            int local;
            do
            {
                local = await self.ReadAsync(buffer, cancel);
                total += local;
                buffer = buffer[local..];
            } while (local > 0 && total < minimumBytes);
            if (throwEndOfStream && total < minimumBytes && !cancel.IsCancellationRequested)
                throw new EndOfStreamException();
            return total;
        }

        /// <summary>
        /// Asynchronously, but completely, fill a buffer or throw an exception if not enough bytes.
        /// </summary>
        /// <param name="buffer">The buffer to fill.</param>
        /// <param name="cancel">A cancelation token.</param>
        /// <param name="throwEndOfStream">If true, will throw if there is not enough data to fill the buffer</param>
        /// <returns></returns>
        public ValueTask<int> ReadExactAsync(Memory<byte> buffer, CancellationToken cancel = default, bool throwEndOfStream = true) =>
            self.ReadAtLeastAsync(buffer, buffer.Length, cancel, throwEndOfStream);
    }
}
