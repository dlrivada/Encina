using BenchmarkDotNet.Attributes;

namespace Encina.Caching.Memory.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for the <see cref="MemoryCacheProvider"/> write operations: set, remove and
/// set with sliding expiration.
/// </summary>
/// <remarks>
/// Split from <see cref="MemoryCacheProviderBenchmarks"/> so each class runs as its own matrix job
/// under the 60-minute limit (#2554). <c>SetAsync</c> is the baseline of this class.
/// </remarks>
[MemoryDiagnoser]
[BenchmarkCategory("Unstable")]
public class MemoryCacheWriteBenchmarks : MemoryCacheBenchmarkBase
{
    [BenchmarkCategory("DocRef:bench:caching-memory/set")]
    [Benchmark(Baseline = true)]
    public async Task SetAsync()
    {
        var key = $"set-{Guid.NewGuid():N}";
        await Provider.SetAsync(key, Data, TimeSpan.FromMinutes(5), CancellationToken.None);
    }

    [BenchmarkCategory("DocRef:bench:caching-memory/remove")]
    [Benchmark]
    public async Task RemoveAsync()
    {
        var key = $"remove-{Guid.NewGuid():N}";
        await Provider.SetAsync(key, Data, TimeSpan.FromMinutes(5), CancellationToken.None);
        await Provider.RemoveAsync(key, CancellationToken.None);
    }

    [BenchmarkCategory("DocRef:bench:caching-memory/set-sliding")]
    [Benchmark]
    public async Task SetWithSlidingExpirationAsync()
    {
        var key = $"sliding-{Guid.NewGuid():N}";
        await Provider.SetWithSlidingExpirationAsync(
            key,
            Data,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(5),
            CancellationToken.None);
    }
}
