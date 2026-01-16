using BenchmarkDotNet.Attributes;
using Encina.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.Caching.Benchmarks;

/// <summary>
/// Benchmarks for MemoryDistributedLockProvider operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class DistributedLockBenchmarks
{
    private MemoryDistributedLockProvider _provider = null!;

    [GlobalSetup]
    public void Setup()
    {
        _provider = new MemoryDistributedLockProvider(NullLogger<MemoryDistributedLockProvider>.Instance);
    }

    [Benchmark(Baseline = true)]
    public async Task<bool> AcquireAndReleaseLock()
    {
        var resource = $"lock-{Guid.NewGuid():N}";
        await using var handle = await _provider.AcquireAsync(resource, TimeSpan.FromSeconds(30), CancellationToken.None);
        return handle is not null;
    }

    [Benchmark]
    public async Task<bool> TryAcquireAsync_Success()
    {
        var resource = $"try-{Guid.NewGuid():N}";
        await using var handle = await _provider.TryAcquireAsync(
            resource,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(10),
            CancellationToken.None);
        return handle is not null;
    }

    [Benchmark]
    public async Task<bool> IsLockedAsync_NotLocked()
    {
        var resource = $"check-{Guid.NewGuid():N}";
        return await _provider.IsLockedAsync(resource, CancellationToken.None);
    }

    [Benchmark]
    public async Task<bool> IsLockedAsync_Locked()
    {
        var resource = $"locked-{Guid.NewGuid():N}";
        await using var handle = await _provider.AcquireAsync(resource, TimeSpan.FromSeconds(30), CancellationToken.None);
        return await _provider.IsLockedAsync(resource, CancellationToken.None);
    }

    [Benchmark]
    public async Task<bool> ExtendLock()
    {
        var resource = $"extend-{Guid.NewGuid():N}";
        await using var handle = await _provider.AcquireAsync(resource, TimeSpan.FromSeconds(10), CancellationToken.None);
        return await _provider.ExtendAsync(resource, TimeSpan.FromSeconds(30), CancellationToken.None);
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    [Arguments(100)]
    public async Task ConcurrentLockContention(int concurrencyLevel)
    {
        var resource = $"contention-{Guid.NewGuid():N}";
        var tasks = new Task[concurrencyLevel];

        for (var i = 0; i < concurrencyLevel; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                var handle = await _provider.TryAcquireAsync(
                    resource,
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromMilliseconds(100),
                    TimeSpan.FromMilliseconds(5),
                    CancellationToken.None);
                if (handle is not null)
                {
                    await Task.Delay(1);
                    await handle.DisposeAsync();
                }
            });
        }

        await Task.WhenAll(tasks);
    }
}
