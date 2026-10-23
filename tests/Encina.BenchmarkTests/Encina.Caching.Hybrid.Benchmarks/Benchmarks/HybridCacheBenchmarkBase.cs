using BenchmarkDotNet.Attributes;
using Encina.Caching.Hybrid;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.Caching.Hybrid.Benchmarks.Benchmarks;

/// <summary>
/// Shared setup and fields for the <see cref="HybridCacheProvider"/> benchmarks.
/// </summary>
/// <remarks>
/// The provider benchmarks are split into several classes so that each one stays well under the
/// 60-minute job limit of the scheduled Benchmarks matrix (#2554); each concrete class becomes its
/// own matrix job. This base carries no benchmark attributes of its own, so the class discovery
/// and BenchmarkDotNet do not treat it as a benchmark class.
/// </remarks>
public abstract class HybridCacheBenchmarkBase : IDisposable
{
    private ServiceProvider _serviceProvider = null!;
    private bool _disposed;

    protected HybridCacheProvider Provider { get; private set; } = null!;
    protected string ExistingKey { get; private set; } = null!;
    protected string MissingKey { get; private set; } = null!;
    protected TestData Data { get; private set; } = null!;

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

        Provider = new HybridCacheProvider(hybridCache, options, NullLogger<HybridCacheProvider>.Instance);
        ExistingKey = "existing-key";
        MissingKey = "missing-key";
        Data = new TestData(Guid.NewGuid(), "Test Name", 42);

        Provider.SetAsync(ExistingKey, Data, TimeSpan.FromMinutes(5), CancellationToken.None)
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
}

/// <summary>Test data record for cache benchmarks.</summary>
public sealed record TestData(Guid Id, string Name, int Value);
