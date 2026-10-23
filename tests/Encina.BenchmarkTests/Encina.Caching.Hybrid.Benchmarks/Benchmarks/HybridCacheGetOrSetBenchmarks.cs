using BenchmarkDotNet.Attributes;

namespace Encina.Caching.Hybrid.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for the <see cref="HybridCacheProvider"/> get-or-set operations: hit, miss and tagged.
/// </summary>
/// <remarks>
/// Split from <see cref="HybridCacheProviderBenchmarks"/> so each class runs as its own matrix job
/// under the 60-minute limit (#2554).
/// </remarks>
[MemoryDiagnoser]
public class HybridCacheGetOrSetBenchmarks : HybridCacheBenchmarkBase
{
    [BenchmarkCategory("DocRef:bench:caching-hybrid/getorset-hit")]
    [Benchmark(Baseline = true)]
    public async Task<TestData> GetOrSetAsync_CacheHit()
    {
        return await Provider.GetOrSetAsync(
            ExistingKey,
            _ => Task.FromResult(Data),
            TimeSpan.FromMinutes(5),
            CancellationToken.None);
    }

    [BenchmarkCategory("DocRef:bench:caching-hybrid/getorset-miss")]
    [Benchmark]
    public async Task<TestData> GetOrSetAsync_CacheMiss()
    {
        var key = $"getorset-{Guid.NewGuid():N}";
        return await Provider.GetOrSetAsync(
            key,
            _ => Task.FromResult(Data),
            TimeSpan.FromMinutes(5),
            CancellationToken.None);
    }

    [BenchmarkCategory("DocRef:bench:caching-hybrid/getorset-tags")]
    [Benchmark]
    public async Task<TestData> GetOrSetAsync_WithTags()
    {
        var key = $"tagged-{Guid.NewGuid():N}";
        return await Provider.GetOrSetAsync(
            key,
            _ => Task.FromResult(Data),
            TimeSpan.FromMinutes(5),
            ["tag1", "tag2"],
            CancellationToken.None);
    }
}
