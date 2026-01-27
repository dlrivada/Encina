using Encina.Caching;
using Encina.Caching.Memory;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using EncinaMemoryCacheOptions = Encina.Caching.Memory.MemoryCacheOptions;

namespace Encina.NBomber.Scenarios.Caching.Providers;

/// <summary>
/// Factory for creating in-memory cache providers for load testing.
/// </summary>
public sealed class MemoryCacheProviderFactory : CacheProviderFactoryBase
{
    private IMemoryCache? _memoryCache;
    private ServiceProvider? _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCacheProviderFactory"/> class.
    /// </summary>
    /// <param name="configureOptions">Optional delegate to configure options.</param>
    public MemoryCacheProviderFactory(Action<CacheProviderOptions>? configureOptions = null)
        : base(configureOptions)
    {
    }

    /// <inheritdoc/>
    public override string ProviderName => "memory";

    /// <inheritdoc/>
    public override CacheProviderCategory Category => CacheProviderCategory.Memory;

    /// <inheritdoc/>
    protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();

        // Configure memory cache
        // NOTE: We don't set SizeLimit because MemoryCacheProvider doesn't set Size on entries.
        // IMemoryCache requires Size to be set on every entry when SizeLimit is configured.
        // For load testing eviction behavior, we rely on expiration instead.
        services.AddMemoryCache(options =>
        {
            options.ExpirationScanFrequency = TimeSpan.FromSeconds(30);
        });

        // Add logging
        services.AddLogging();

        // Configure Encina memory cache options
        services.Configure<EncinaMemoryCacheOptions>(options =>
        {
            options.DefaultExpiration = Options.DefaultExpiration;
        });

        _serviceProvider = services.BuildServiceProvider();
        _memoryCache = _serviceProvider.GetRequiredService<IMemoryCache>();

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override ICacheProvider CreateCacheProvider()
    {
        EnsureInitialized();

        return new MemoryCacheProvider(
            _memoryCache!,
            Microsoft.Extensions.Options.Options.Create(new EncinaMemoryCacheOptions
            {
                DefaultExpiration = Options.DefaultExpiration
            }),
            NullLogger<MemoryCacheProvider>.Instance);
    }

    /// <inheritdoc/>
    protected override async Task DisposeCoreAsync()
    {
        // Only dispose the service provider - it will handle disposing the memory cache.
        // Don't dispose _memoryCache directly to avoid double-dispose.
        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync().ConfigureAwait(false);
            _serviceProvider = null;
            _memoryCache = null;
        }
    }
}
