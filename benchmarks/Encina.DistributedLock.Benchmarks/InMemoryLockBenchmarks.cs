using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.DistributedLock.Benchmarks;

/// <summary>
/// Benchmarks for InMemory distributed lock provider.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class InMemoryLockBenchmarks
{
    private InMemoryDistributedLockProvider _provider = null!;
    private int _resourceCounter;

    [GlobalSetup]
    public void Setup()
    {
        _provider = new InMemoryDistributedLockProvider(
            Options.Create(new InMemoryLockOptions()),
            NullLogger<InMemoryDistributedLockProvider>.Instance);
        _resourceCounter = 0;
    }

    [Benchmark(Baseline = true)]
    public async Task<IAsyncDisposable?> TryAcquireAsync_SingleLock()
    {
        var resource = $"benchmark-{Interlocked.Increment(ref _resourceCounter)}";
        var lockHandle = await _provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        if (lockHandle is not null)
        {
            await lockHandle.DisposeAsync();
        }

        return lockHandle;
    }

    [Benchmark]
    public async Task<bool> IsLockedAsync_UnlockedResource()
    {
        var resource = $"benchmark-is-locked-{Interlocked.Increment(ref _resourceCounter)}";
        return await _provider.IsLockedAsync(resource, CancellationToken.None);
    }

    [Benchmark]
    public async Task<bool> IsLockedAsync_LockedResource()
    {
        var resource = $"benchmark-locked-{Interlocked.Increment(ref _resourceCounter)}";

        await using var lockHandle = await _provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        return await _provider.IsLockedAsync(resource, CancellationToken.None);
    }

    [Benchmark]
    public async Task AcquireAndRelease_100Iterations()
    {
        for (var i = 0; i < 100; i++)
        {
            var resource = $"benchmark-iteration-{Interlocked.Increment(ref _resourceCounter)}";
            var lockHandle = await _provider.TryAcquireAsync(
                resource,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(50),
                CancellationToken.None);

            await lockHandle!.DisposeAsync();
        }
    }

    [Benchmark]
    public async Task ParallelAcquire_10DifferentResources()
    {
        var baseCounter = Interlocked.Add(ref _resourceCounter, 10);
        var tasks = Enumerable.Range(baseCounter - 10, 10)
            .Select(async i =>
            {
                var lockHandle = await _provider.TryAcquireAsync(
                    $"benchmark-parallel-{i}",
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromMilliseconds(50),
                    CancellationToken.None);

                await lockHandle!.DisposeAsync();
            });

        await Task.WhenAll(tasks);
    }
}
