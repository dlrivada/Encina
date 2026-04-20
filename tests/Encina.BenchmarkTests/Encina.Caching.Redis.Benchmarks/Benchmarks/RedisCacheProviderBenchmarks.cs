using BenchmarkDotNet.Attributes;
using Encina.Caching.Redis;
using Encina.Caching.Redis.Benchmarks.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.Caching.Redis.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for <see cref="RedisCacheProvider"/> operations.
/// Uses Testcontainers to spin up a local Redis 7 container.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 5, iterationCount: 20)]
public class RedisCacheProviderBenchmarks : IDisposable
{
    private RedisBenchmarkContainer _container = null!;
    private RedisCacheProvider _provider = null!;
    private bool _disposed;
    private string _existingKey = null!;
    private string _missingKey = null!;
    private string _removeKey = null!;
    private string _patternPrefix = null!;
    private TestData _testData = null!;

    [GlobalSetup]
    public void Setup()
    {
        _container = new RedisBenchmarkContainer();
        _container.Start();

        var options = Options.Create(new RedisCacheOptions
        {
            DefaultExpiration = TimeSpan.FromMinutes(5),
            KeyPrefix = "bench"
        });

        _provider = new RedisCacheProvider(
            _container.Connection,
            options,
            NullLogger<RedisCacheProvider>.Instance);

        _existingKey = "existing-key";
        _missingKey = "missing-key";
        _testData = new TestData(Guid.NewGuid(), "Test Name", 42);

        _provider.SetAsync(_existingKey, _testData, TimeSpan.FromMinutes(5), CancellationToken.None)
            .GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _container?.Stop();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    [BenchmarkCategory("DocRef:bench:caching-redis/get-hit")]
    [Benchmark(Baseline = true)]
    public async Task<TestData?> GetAsync_CacheHit()
    {
        return await _provider.GetAsync<TestData>(_existingKey, CancellationToken.None);
    }

    [BenchmarkCategory("DocRef:bench:caching-redis/get-miss")]
    [Benchmark]
    public async Task<TestData?> GetAsync_CacheMiss()
    {
        return await _provider.GetAsync<TestData>(_missingKey, CancellationToken.None);
    }

    [BenchmarkCategory("DocRef:bench:caching-redis/set")]
    [Benchmark]
    public async Task SetAsync()
    {
        // Reuse a fixed key (overwrite in-place) to avoid polluting the keyspace
        // across iterations — measured op is the SET round-trip, not key churn.
        await _provider.SetAsync("set-benchmark", _testData, TimeSpan.FromMinutes(5), CancellationToken.None);
    }

    [BenchmarkCategory("DocRef:bench:caching-redis/exists-true")]
    [Benchmark]
    public async Task<bool> ExistsAsync_True()
    {
        return await _provider.ExistsAsync(_existingKey, CancellationToken.None);
    }

    [BenchmarkCategory("DocRef:bench:caching-redis/exists-false")]
    [Benchmark]
    public async Task<bool> ExistsAsync_False()
    {
        return await _provider.ExistsAsync(_missingKey, CancellationToken.None);
    }

    [BenchmarkCategory("DocRef:bench:caching-redis/getorset-hit")]
    [Benchmark]
    public async Task<TestData> GetOrSetAsync_CacheHit()
    {
        return await _provider.GetOrSetAsync(
            _existingKey,
            _ => Task.FromResult(_testData),
            TimeSpan.FromMinutes(5),
            CancellationToken.None);
    }

    [IterationSetup(Target = nameof(GetOrSetAsync_CacheMiss))]
    public void EnsureGetOrSetMissKeyAbsent()
    {
        // Reuse a fixed key, but clear it before each iteration so the
        // measured op always hits the miss path.
        _provider.RemoveAsync("getorset-miss-benchmark", CancellationToken.None)
            .GetAwaiter().GetResult();
    }

    [BenchmarkCategory("DocRef:bench:caching-redis/getorset-miss")]
    [Benchmark]
    public async Task<TestData> GetOrSetAsync_CacheMiss()
    {
        return await _provider.GetOrSetAsync(
            "getorset-miss-benchmark",
            _ => Task.FromResult(_testData),
            TimeSpan.FromMinutes(5),
            CancellationToken.None);
    }

    [IterationSetup(Target = nameof(RemoveAsync))]
    public void SeedRemoveKey()
    {
        // Reuse a fixed key; re-seed before each iteration so RemoveAsync
        // always has something to delete.
        _removeKey = "remove-benchmark";
        _provider.SetAsync(_removeKey, _testData, TimeSpan.FromMinutes(5), CancellationToken.None)
            .GetAwaiter().GetResult();
    }

    [BenchmarkCategory("DocRef:bench:caching-redis/remove")]
    [Benchmark]
    public async Task RemoveAsync()
    {
        await _provider.RemoveAsync(_removeKey, CancellationToken.None);
    }

    [IterationSetup(Target = nameof(RemoveByPatternAsync))]
    public void SeedPatternKeys()
    {
        // Fixed prefix; re-seed before each iteration so the SCAN+DEL has a
        // stable 5-key dataset to operate on.
        _patternPrefix = "pattern-benchmark";
        for (var i = 0; i < 5; i++)
        {
            _provider.SetAsync($"{_patternPrefix}-{i}", _testData, TimeSpan.FromMinutes(5), CancellationToken.None)
                .GetAwaiter().GetResult();
        }
    }

    [BenchmarkCategory("DocRef:bench:caching-redis/remove-by-pattern")]
    [Benchmark]
    public async Task RemoveByPatternAsync()
    {
        await _provider.RemoveByPatternAsync($"{_patternPrefix}-*", CancellationToken.None);
    }

    [BenchmarkCategory("DocRef:bench:caching-redis/set-sliding")]
    [Benchmark]
    public async Task SetWithSlidingExpirationAsync()
    {
        // Reuse a fixed key (overwrite in-place) — see SetAsync comment.
        await _provider.SetWithSlidingExpirationAsync(
            "sliding-benchmark",
            _testData,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(5),
            CancellationToken.None);
    }
}

/// <summary>Test data record for cache benchmarks.</summary>
public sealed record TestData(Guid Id, string Name, int Value);
