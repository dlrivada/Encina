using BenchmarkDotNet.Attributes;
using Encina.Caching.Hybrid;

namespace Encina.Caching.Hybrid.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for the <see cref="HybridCacheProvider"/> read operations (.NET 10 multi-tier cache):
/// get and exists, hit and miss.
/// </summary>
/// <remarks>
/// Split into <see cref="HybridCacheGetOrSetBenchmarks"/> and <see cref="HybridCacheWriteBenchmarks"/>
/// so each class runs as its own matrix job under the 60-minute limit (#2554). This class keeps its
/// original name so the series of the benchmarks it still contains keep their dashboard identity.
/// </remarks>
[MemoryDiagnoser]
public class HybridCacheProviderBenchmarks : HybridCacheBenchmarkBase
{
    [BenchmarkCategory("DocRef:bench:caching-hybrid/get-hit")]
    [Benchmark(Baseline = true)]
    public async Task<TestData?> GetAsync_CacheHit()
    {
        return await Provider.GetAsync<TestData>(ExistingKey, CancellationToken.None);
    }

    [BenchmarkCategory("DocRef:bench:caching-hybrid/get-miss")]
    [Benchmark]
    public async Task<TestData?> GetAsync_CacheMiss()
    {
        return await Provider.GetAsync<TestData>(MissingKey, CancellationToken.None);
    }

    [BenchmarkCategory("DocRef:bench:caching-hybrid/exists-true")]
    [Benchmark]
    public async Task<bool> ExistsAsync_True()
    {
        return await Provider.ExistsAsync(ExistingKey, CancellationToken.None);
    }

    [BenchmarkCategory("DocRef:bench:caching-hybrid/exists-false")]
    [Benchmark]
    public async Task<bool> ExistsAsync_False()
    {
        return await Provider.ExistsAsync(MissingKey, CancellationToken.None);
    }
}
