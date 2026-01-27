using Encina.NBomber.Scenarios.Caching.Providers;
using NBomber.Contracts;

namespace Encina.NBomber.Scenarios.Caching;

/// <summary>
/// Runner for caching load test scenarios.
/// Creates and executes Memory, Redis, and Hybrid cache scenarios.
/// </summary>
public sealed class CachingScenarioRunner : IAsyncDisposable
{
    private readonly CachingFeature _feature;
    private readonly string _providerName;
    private ICacheProviderFactory? _providerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingScenarioRunner"/> class.
    /// </summary>
    /// <param name="feature">The caching feature to test.</param>
    /// <param name="providerName">Optional provider name (for Redis variants).</param>
    public CachingScenarioRunner(CachingFeature feature, string? providerName = null)
    {
        _feature = feature;
        _providerName = providerName ?? GetDefaultProviderName(feature);
    }

    /// <summary>
    /// Creates scenarios for the specified caching feature.
    /// </summary>
    /// <returns>A collection of scenario props to run.</returns>
    public async Task<ScenarioProps[]> CreateScenariosAsync()
    {
        var scenarios = new List<ScenarioProps>();

        _providerFactory = CacheProviderRegistry.CreateFactory(_providerName, options =>
        {
            options.DefaultExpiration = TimeSpan.FromMinutes(5);
            options.MaxCacheSize = 10000;
            options.ValueSizeBytes = 1024;
            options.KeyPrefix = "nbomber";
        });

        await _providerFactory.InitializeAsync().ConfigureAwait(false);

        if (!_providerFactory.IsAvailable)
        {
            Console.WriteLine($"Cache provider '{_providerName}' is not available (Docker may be required).");
            return [];
        }

        var context = new CacheScenarioContext(_providerFactory, _providerName);

        if (_feature is CachingFeature.Memory or CachingFeature.All)
        {
            if (_providerFactory.Category == CacheProviderCategory.Memory)
            {
                var memoryFactory = new MemoryCacheScenarioFactory(context);
                scenarios.AddRange(memoryFactory.CreateScenarios());
            }
        }

        if (_feature is CachingFeature.Redis or CachingFeature.All)
        {
            if (_providerFactory.Category == CacheProviderCategory.Redis)
            {
                var redisFactory = new RedisCacheScenarioFactory(context);
                scenarios.AddRange(redisFactory.CreateScenarios());
            }
        }

        if (_feature is CachingFeature.Hybrid or CachingFeature.All)
        {
            if (_providerFactory.Category == CacheProviderCategory.Hybrid)
            {
                var hybridFactory = new HybridCacheScenarioFactory(context);
                scenarios.AddRange(hybridFactory.CreateScenarios());
            }
        }

        return scenarios.ToArray();
    }

    /// <summary>
    /// Creates scenarios for all available providers.
    /// </summary>
    /// <returns>A collection of scenario props to run.</returns>
    public static async Task<ScenarioProps[]> CreateAllProvidersAsync()
    {
        var allScenarios = new List<ScenarioProps>();

        // Memory scenarios
        var memoryRunner = new CachingScenarioRunner(CachingFeature.Memory, CacheProviderRegistry.ProviderNames.Memory);
        allScenarios.AddRange(await memoryRunner.CreateScenariosAsync().ConfigureAwait(false));
        await memoryRunner.DisposeAsync().ConfigureAwait(false);

        // Redis scenarios
        var redisRunner = new CachingScenarioRunner(CachingFeature.Redis, CacheProviderRegistry.ProviderNames.Redis);
        allScenarios.AddRange(await redisRunner.CreateScenariosAsync().ConfigureAwait(false));
        await redisRunner.DisposeAsync().ConfigureAwait(false);

        // Hybrid scenarios
        var hybridRunner = new CachingScenarioRunner(CachingFeature.Hybrid, CacheProviderRegistry.ProviderNames.Hybrid);
        allScenarios.AddRange(await hybridRunner.CreateScenariosAsync().ConfigureAwait(false));
        await hybridRunner.DisposeAsync().ConfigureAwait(false);

        return allScenarios.ToArray();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_providerFactory is not null)
        {
            await _providerFactory.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static string GetDefaultProviderName(CachingFeature feature)
    {
        return feature switch
        {
            CachingFeature.Memory => CacheProviderRegistry.ProviderNames.Memory,
            CachingFeature.Redis => CacheProviderRegistry.ProviderNames.Redis,
            CachingFeature.Hybrid => CacheProviderRegistry.ProviderNames.Hybrid,
            _ => CacheProviderRegistry.ProviderNames.Memory
        };
    }
}

/// <summary>
/// Feature categories for caching load testing.
/// </summary>
public enum CachingFeature
{
    /// <summary>In-memory cache (IMemoryCache).</summary>
    Memory,

    /// <summary>Redis distributed cache.</summary>
    Redis,

    /// <summary>Hybrid L1/L2 cache (HybridCache).</summary>
    Hybrid,

    /// <summary>All caching features.</summary>
    All
}
