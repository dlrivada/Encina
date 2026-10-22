using BenchmarkDotNet.Attributes;
using Encina.Caching.Memory;

namespace Encina.Caching.Memory.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for the <see cref="MemoryCacheProvider"/> read operations (in-process L1 cache):
/// get and exists, hit and miss.
/// </summary>
/// <remarks>
/// Split into <see cref="MemoryCacheGetOrSetBenchmarks"/> and <see cref="MemoryCacheWriteBenchmarks"/>
/// so each class runs as its own matrix job under the 60-minute limit (#2554). This class keeps its
/// original name so the series of the benchmarks it still contains keep their dashboard identity.
/// </remarks>
[MemoryDiagnoser]
[BenchmarkCategory("Unstable")]
public class MemoryCacheProviderBenchmarks : MemoryCacheBenchmarkBase
{
    [BenchmarkCategory("DocRef:bench:caching-memory/get-hit")]
    [Benchmark(Baseline = true)]
    public async Task<TestData?> GetAsync_CacheHit()
    {
        return await Provider.GetAsync<TestData>(ExistingKey, CancellationToken.None);
    }

    [BenchmarkCategory("DocRef:bench:caching-memory/get-miss")]
    [Benchmark]
    public async Task<TestData?> GetAsync_CacheMiss()
    {
        return await Provider.GetAsync<TestData>(MissingKey, CancellationToken.None);
    }

    [BenchmarkCategory("DocRef:bench:caching-memory/exists-true")]
    [Benchmark]
    public async Task<bool> ExistsAsync_True()
    {
        return await Provider.ExistsAsync(ExistingKey, CancellationToken.None);
    }

    [BenchmarkCategory("DocRef:bench:caching-memory/exists-false")]
    [Benchmark]
    public async Task<bool> ExistsAsync_False()
    {
        return await Provider.ExistsAsync(MissingKey, CancellationToken.None);
    }
}
