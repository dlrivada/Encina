using BenchmarkDotNet.Attributes;

namespace Encina.Caching.Hybrid.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for the <see cref="HybridCacheProvider"/> write operations: set, remove,
/// remove by tag and set with sliding expiration.
/// </summary>
/// <remarks>
/// Split from <see cref="HybridCacheProviderBenchmarks"/> so each class runs as its own matrix job
/// under the 60-minute limit (#2554). <c>SetAsync</c> is the baseline of this class.
/// </remarks>
[MemoryDiagnoser]
public class HybridCacheWriteBenchmarks : HybridCacheBenchmarkBase
{
    [BenchmarkCategory("DocRef:bench:caching-hybrid/set")]
    [Benchmark(Baseline = true)]
    public async Task SetAsync()
    {
        var key = $"set-{Guid.NewGuid():N}";
        await Provider.SetAsync(key, Data, TimeSpan.FromMinutes(5), CancellationToken.None);
    }

    [BenchmarkCategory("DocRef:bench:caching-hybrid/remove")]
    [Benchmark]
    public async Task RemoveAsync()
    {
        var key = $"remove-{Guid.NewGuid():N}";
        await Provider.SetAsync(key, Data, TimeSpan.FromMinutes(5), CancellationToken.None);
        await Provider.RemoveAsync(key, CancellationToken.None);
    }

    [BenchmarkCategory("DocRef:bench:caching-hybrid/remove-by-tag")]
    [Benchmark]
    public async Task RemoveByTagAsync()
    {
        var tag = $"tag-{Guid.NewGuid():N}";
        var key = $"taggeditem-{Guid.NewGuid():N}";

        await Provider.GetOrSetAsync(
            key,
            _ => Task.FromResult(Data),
            TimeSpan.FromMinutes(5),
            [tag],
            CancellationToken.None);

        await Provider.RemoveByTagAsync(tag, CancellationToken.None);
    }

    [BenchmarkCategory("DocRef:bench:caching-hybrid/set-sliding")]
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
