using Melville.StreamInterfaces;
using Melville.StreamInterfaces.Memory;
using System;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace IntegrationTesting.Streams;

public class CopyToMethodTests
{
    private static readonly byte[] source = Enumerable.Range(0, 200).Select(i => (byte)i).ToArray();
    private readonly ISyncReader reader = new MemoryReader(source);
    private readonly MemoryReaderWriter target = new MemoryReaderWriter();

    [Test]
    [Arguments(1)]
    [Arguments(20)]
    [Arguments(1500)]
    public void SyncCopy(int size)
    {
        reader.CopyTo(target, size);
        target.AsSpan().SequenceEqual(source).Should().BeTrue();
    }

    [Test]
    public void OneParamSyncCopy()
    {
        reader.CopyTo(target);
        target.AsSpan().SequenceEqual(source).Should().BeTrue();
    }
    
    [Test]
    [Arguments(1)]
    [Arguments(20)]
    [Arguments(1500)]
    public async Task AsyncCopy(int size)
    {
        await ((IAsyncReader)reader).CopyToAsync(target, size);
        target.AsSpan().SequenceEqual(source).Should().BeTrue();
    }

    [Test]
    public async Task OneParamAsyncCopy()
    {
        await ((IAsyncReader)reader).CopyToAsync(target);
        target.AsSpan().SequenceEqual(source).Should().BeTrue();
    }
}
