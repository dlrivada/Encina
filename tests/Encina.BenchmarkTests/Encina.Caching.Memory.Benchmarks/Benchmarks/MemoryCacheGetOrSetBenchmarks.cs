using BenchmarkDotNet.Attributes;

namespace Encina.Caching.Memory.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for the <see cref="MemoryCacheProvider"/> get-or-set operations: hit and miss.
/// </summary>
/// <remarks>
/// Split from <see cref="MemoryCacheProviderBenchmarks"/> so each class runs as its own matrix job
/// under the 60-minute limit (#2554).
/// </remarks>
[MemoryDiagnoser]
[BenchmarkCategory("Unstable")]
public class MemoryCacheGetOrSetBenchmarks : MemoryCacheBenchmarkBase
{
    [BenchmarkCategory("DocRef:bench:caching-memory/getorset-hit")]
    [Benchmark(Baseline = true)]
    public async Task<TestData> GetOrSetAsync_CacheHit()
    {
        return await Provider.GetOrSetAsync(
            ExistingKey,
            _ => Task.FromResult(Data),
            TimeSpan.FromMinutes(5),
            CancellationToken.None);
    }

    [BenchmarkCategory("DocRef:bench:caching-memory/getorset-miss")]
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
}
