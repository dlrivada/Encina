using Encina.Caching;
using Encina.Caching.Hybrid;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Encina.NBomber.Scenarios.Caching.Providers;

/// <summary>
/// Factory for creating hybrid L1/L2 cache providers for load testing.
/// Uses Microsoft HybridCache with memory (L1) and Redis (L2).
/// </summary>
public sealed class HybridCacheProviderFactory : CacheProviderFactoryBase
{
    private RedisContainer? _container;
    private ConnectionMultiplexer? _connection;
    private ServiceProvider? _serviceProvider;
    private HybridCache? _hybridCache;
    private bool _containerInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="HybridCacheProviderFactory"/> class.
    /// </summary>
    /// <param name="configureOptions">Optional delegate to configure options.</param>
    public HybridCacheProviderFactory(Action<CacheProviderOptions>? configureOptions = null)
        : base(configureOptions)
    {
    }

    /// <inheritdoc/>
    public override string ProviderName => "hybrid";

    /// <inheritdoc/>
    public override CacheProviderCategory Category => CacheProviderCategory.Hybrid;

    /// <inheritdoc/>
    public override bool IsAvailable => base.IsAvailable && _containerInitialized;

    /// <summary>
    /// Gets the Redis connection multiplexer for direct L2 access.
    /// </summary>
    public IConnectionMultiplexer? Connection => _connection;

    /// <inheritdoc/>
    protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        // Start Redis container for L2
        try
        {
            if (!string.IsNullOrEmpty(Options.RedisConnectionString))
            {
                _connection = await ConnectionMultiplexer.ConnectAsync(Options.RedisConnectionString)
                    .ConfigureAwait(false);
            }
            else
            {
                _container = new RedisBuilder(Options.RedisImage)
                    .WithCleanUp(true)
                    .Build();

                await _container.StartAsync(cancellationToken).ConfigureAwait(false);

                var connectionString = _container.GetConnectionString();
                _connection = await ConnectionMultiplexer.ConnectAsync(connectionString).ConfigureAwait(false);
            }

            // Build service provider with HybridCache
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging();

            // Add memory cache for L1
            services.AddMemoryCache(options =>
            {
                options.SizeLimit = Options.MaxCacheSize;
            });

            // Add Redis for L2
            services.AddSingleton<IConnectionMultiplexer>(_connection);
            services.AddStackExchangeRedisCache(options =>
            {
                options.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(_connection);
            });

            // Add HybridCache
#pragma warning disable EXTEXP0018 // HybridCache is experimental
            services.AddHybridCache(options =>
            {
                options.DefaultEntryOptions = new HybridCacheEntryOptions
                {
                    Expiration = Options.DefaultExpiration,
                    LocalCacheExpiration = Options.L1CacheExpiration ?? Options.DefaultExpiration
                };
            });
#pragma warning restore EXTEXP0018

            _serviceProvider = services.BuildServiceProvider();
            _hybridCache = _serviceProvider.GetRequiredService<HybridCache>();
            _containerInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize HybridCache: {ex.Message}");
            _containerInitialized = false;
        }
    }

    /// <inheritdoc/>
    public override ICacheProvider CreateCacheProvider()
    {
        EnsureInitialized();

        if (_hybridCache is null)
        {
            throw new InvalidOperationException("HybridCache is not available.");
        }

        return new HybridCacheProvider(
            _hybridCache,
            Microsoft.Extensions.Options.Options.Create(new HybridCacheProviderOptions
            {
                DefaultExpiration = Options.DefaultExpiration,
                LocalCacheExpiration = Options.L1CacheExpiration
            }),
            NullLogger<HybridCacheProvider>.Instance);
    }

    /// <summary>
    /// Flushes all keys from the Redis L2 cache.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task FlushL2CacheAsync()
    {
        if (_connection is null)
        {
            return;
        }

        var server = _connection.GetServer(_connection.GetEndPoints()[0]);
        await server.FlushDatabaseAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    protected override async Task DisposeCoreAsync()
    {
        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync().ConfigureAwait(false);
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync().ConfigureAwait(false);
            _connection.Dispose();
        }

        if (_container is not null)
        {
            await _container.StopAsync().ConfigureAwait(false);
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }
}
