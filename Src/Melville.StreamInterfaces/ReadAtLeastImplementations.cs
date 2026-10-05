using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Melville.StreamInterfaces;

public static class ReadAtLeastImplementations
{
    extension(ISyncReader self)
    {
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

        public int ReadExact(Span<byte> buffer) => self.ReadAtLeast(buffer, buffer.Length, true);
    }

    extension(IAsyncReader self)
    {
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

        public ValueTask<int> ReadExactAsync(Memory<byte> buffer, CancellationToken cancel = default, bool throwEndOfStream = true) =>
            self.ReadAtLeastAsync(buffer, buffer.Length, cancel, throwEndOfStream);
    }
}
