using Encina.Caching;
using Encina.Caching.Redis;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Encina.NBomber.Scenarios.Caching.Providers;

/// <summary>
/// Factory for creating Redis cache providers for load testing.
/// Supports multiple Redis-compatible images (Redis, Valkey, Garnet, etc.).
/// </summary>
public sealed class RedisCacheProviderFactory : CacheProviderFactoryBase
{
    private RedisContainer? _container;
    private ConnectionMultiplexer? _connection;
    private bool _containerInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisCacheProviderFactory"/> class.
    /// </summary>
    /// <param name="configureOptions">Optional delegate to configure options.</param>
    public RedisCacheProviderFactory(Action<CacheProviderOptions>? configureOptions = null)
        : base(configureOptions)
    {
    }

    /// <inheritdoc/>
    public override string ProviderName => GetProviderNameFromImage();

    /// <inheritdoc/>
    public override CacheProviderCategory Category => CacheProviderCategory.Redis;

    /// <inheritdoc/>
    public override bool IsAvailable => base.IsAvailable && _containerInitialized && _connection is not null;

    /// <summary>
    /// Gets the Redis connection multiplexer for direct access.
    /// </summary>
    public IConnectionMultiplexer? Connection => _connection;

    /// <inheritdoc/>
    protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        // Check if we have an existing connection string (for external Redis)
        if (!string.IsNullOrEmpty(Options.RedisConnectionString))
        {
            _connection = await ConnectionMultiplexer.ConnectAsync(Options.RedisConnectionString)
                .ConfigureAwait(false);
            _containerInitialized = true;
            return;
        }

        // Start a container using Testcontainers
        try
        {
            _container = new RedisBuilder(Options.RedisImage)
                .WithCleanUp(true)
                .Build();

            await _container.StartAsync(cancellationToken).ConfigureAwait(false);

            var connectionString = _container.GetConnectionString();
            _connection = await ConnectionMultiplexer.ConnectAsync(connectionString).ConfigureAwait(false);
            _containerInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start Redis container ({Options.RedisImage}): {ex.Message}");
            // Container might not be available (no Docker)
            _containerInitialized = false;
        }
    }

    /// <inheritdoc/>
    public override ICacheProvider CreateCacheProvider()
    {
        EnsureInitialized();

        if (_connection is null)
        {
            throw new InvalidOperationException("Redis connection is not available.");
        }

        return new RedisCacheProvider(
            _connection,
            Microsoft.Extensions.Options.Options.Create(new RedisCacheOptions
            {
                DefaultExpiration = Options.DefaultExpiration,
                KeyPrefix = Options.KeyPrefix
            }),
            NullLogger<RedisCacheProvider>.Instance);
    }

    /// <summary>
    /// Flushes all keys from the Redis database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task FlushDatabaseAsync()
    {
        if (_connection is null)
        {
            return;
        }

        var server = _connection.GetServer(_connection.GetEndPoints()[0]);
        await server.FlushDatabaseAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the database instance for direct Redis operations.
    /// </summary>
    /// <returns>The Redis database.</returns>
    public IDatabase? GetDatabase() => _connection?.GetDatabase();

    /// <inheritdoc/>
    protected override async Task DisposeCoreAsync()
    {
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

    private string GetProviderNameFromImage()
    {
        var image = Options.RedisImage.ToLowerInvariant();

        if (image.Contains("valkey", StringComparison.Ordinal))
        {
            return "redis-valkey";
        }

        if (image.Contains("garnet", StringComparison.Ordinal))
        {
            return "redis-garnet";
        }

        if (image.Contains("dragonfly", StringComparison.Ordinal))
        {
            return "redis-dragonfly";
        }

        if (image.Contains("keydb", StringComparison.Ordinal))
        {
            return "redis-keydb";
        }

        return "redis";
    }
}
