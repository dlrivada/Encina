using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimpleMediator.Caching;
using SimpleMediator.Caching.Memory;
using MsMemoryCache = Microsoft.Extensions.Caching.Memory.MemoryCache;
using MsMemoryCacheOptions = Microsoft.Extensions.Caching.Memory.MemoryCacheOptions;

namespace SimpleMediator.Caching.Benchmarks;

/// <summary>
/// Benchmarks for MemoryCacheProvider operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class MemoryCacheProviderBenchmarks : IDisposable
{
    private MemoryCacheProvider _provider = null!;
    private MsMemoryCache _memoryCache = null!;
    private bool _disposed;
    private string _existingKey = null!;
    private string _missingKey = null!;
    private TestData _testData = null!;

    [GlobalSetup]
    public void Setup()
    {
        var memoryCacheOptions = Options.Create(new MsMemoryCacheOptions());
        _memoryCache = new MsMemoryCache(memoryCacheOptions);

        var options = Options.Create(new MemoryCacheOptions
        {
            DefaultExpiration = TimeSpan.FromMinutes(5)
        });

        _provider = new MemoryCacheProvider(_memoryCache, options, NullLogger<MemoryCacheProvider>.Instance);
        _existingKey = "existing-key";
        _missingKey = "missing-key";
        _testData = new TestData(Guid.NewGuid(), "Test Name", 42);

        // Pre-populate cache
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
            _memoryCache?.Dispose();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    [Benchmark(Baseline = true)]
    public async Task<TestData?> GetAsync_CacheHit()
    {
        return await _provider.GetAsync<TestData>(_existingKey, CancellationToken.None);
    }

    [Benchmark]
    public async Task<TestData?> GetAsync_CacheMiss()
    {
        return await _provider.GetAsync<TestData>(_missingKey, CancellationToken.None);
    }

    [Benchmark]
    public async Task SetAsync()
    {
        var key = $"set-{Guid.NewGuid():N}";
        await _provider.SetAsync(key, _testData, TimeSpan.FromMinutes(5), CancellationToken.None);
    }

    [Benchmark]
    public async Task<bool> ExistsAsync_True()
    {
        return await _provider.ExistsAsync(_existingKey, CancellationToken.None);
    }

    [Benchmark]
    public async Task<bool> ExistsAsync_False()
    {
        return await _provider.ExistsAsync(_missingKey, CancellationToken.None);
    }

    [Benchmark]
    public async Task<TestData> GetOrSetAsync_CacheHit()
    {
        return await _provider.GetOrSetAsync(
            _existingKey,
            _ => Task.FromResult(_testData),
            TimeSpan.FromMinutes(5),
            CancellationToken.None);
    }

    [Benchmark]
    public async Task<TestData> GetOrSetAsync_CacheMiss()
    {
        var key = $"getorset-{Guid.NewGuid():N}";
        return await _provider.GetOrSetAsync(
            key,
            _ => Task.FromResult(_testData),
            TimeSpan.FromMinutes(5),
            CancellationToken.None);
    }

    [Benchmark]
    public async Task RemoveAsync()
    {
        var key = $"remove-{Guid.NewGuid():N}";
        await _provider.SetAsync(key, _testData, TimeSpan.FromMinutes(5), CancellationToken.None);
        await _provider.RemoveAsync(key, CancellationToken.None);
    }

    [Benchmark]
    public async Task SetWithSlidingExpirationAsync()
    {
        var key = $"sliding-{Guid.NewGuid():N}";
        await _provider.SetWithSlidingExpirationAsync(
            key,
            _testData,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(5),
            CancellationToken.None);
    }
}

public sealed record TestData(Guid Id, string Name, int Value);
