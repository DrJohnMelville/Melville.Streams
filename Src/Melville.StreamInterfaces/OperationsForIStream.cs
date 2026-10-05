using System;

namespace Melville.StreamInterfaces;

internal static class OperationsForIStream
{
    extension(IStream self)
    {
        public long PositionOrException =>
          (self as IStreamPosition)?.Position ??
            throw new InvalidOperationException("Can only use SeekOrigin.Current in streams that implement IStreamPosition");
        public long LengthOrException =>
          (self as IStreamLength)?.Length ??
            throw new InvalidOperationException("Can only use SeekOrigin.End in streams that implement IStreamLength");
    }
}
