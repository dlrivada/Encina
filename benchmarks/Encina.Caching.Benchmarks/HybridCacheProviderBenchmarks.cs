using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Encina.Caching.Hybrid;

namespace Encina.Caching.Benchmarks;

/// <summary>
/// Benchmarks for HybridCacheProvider operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class HybridCacheProviderBenchmarks : IDisposable
{
    private HybridCacheProvider _provider = null!;
    private ServiceProvider _serviceProvider = null!;
    private bool _disposed;
    private string _existingKey = null!;
    private string _missingKey = null!;
    private TestData _testData = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        services.AddDistributedMemoryCache();
        _serviceProvider = services.BuildServiceProvider();

        var hybridCache = _serviceProvider.GetRequiredService<HybridCache>();
        var options = Options.Create(new HybridCacheProviderOptions
        {
            DefaultExpiration = TimeSpan.FromMinutes(5)
        });

        _provider = new HybridCacheProvider(hybridCache, options, NullLogger<HybridCacheProvider>.Instance);
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
            _serviceProvider?.Dispose();
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
    public async Task<TestData> GetOrSetAsync_WithTags()
    {
        var key = $"tagged-{Guid.NewGuid():N}";
        return await _provider.GetOrSetAsync(
            key,
            _ => Task.FromResult(_testData),
            TimeSpan.FromMinutes(5),
            ["tag1", "tag2"],
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
    public async Task RemoveByTagAsync()
    {
        var tag = $"tag-{Guid.NewGuid():N}";
        var key = $"taggeditem-{Guid.NewGuid():N}";

        // Add item with tag
        await _provider.GetOrSetAsync(
            key,
            _ => Task.FromResult(_testData),
            TimeSpan.FromMinutes(5),
            [tag],
            CancellationToken.None);

        // Remove by tag
        await _provider.RemoveByTagAsync(tag, CancellationToken.None);
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
