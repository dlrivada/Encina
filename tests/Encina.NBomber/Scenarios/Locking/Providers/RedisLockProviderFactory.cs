using Encina.DistributedLock;
using Encina.DistributedLock.Redis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Encina.NBomber.Scenarios.Locking.Providers;

/// <summary>
/// Factory for creating Redis-based lock providers for load testing.
/// Supports Redis and compatible variants (Valkey, Garnet, Dragonfly, KeyDB).
/// </summary>
public sealed class RedisLockProviderFactory : LockProviderFactoryBase
{
    private RedisContainer? _container;
    private ConnectionMultiplexer? _connection;
    private ServiceProvider? _serviceProvider;
    private bool _containerInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisLockProviderFactory"/> class.
    /// </summary>
    /// <param name="configureOptions">Optional delegate to configure options.</param>
    public RedisLockProviderFactory(Action<LockProviderOptions>? configureOptions = null)
        : base(configureOptions)
    {
    }

    /// <inheritdoc/>
    public override string ProviderName => "redis";

    /// <inheritdoc/>
    public override LockProviderCategory Category => LockProviderCategory.Redis;

    /// <inheritdoc/>
    public override bool IsAvailable => base.IsAvailable && _containerInitialized;

    /// <summary>
    /// Gets the Redis connection multiplexer.
    /// </summary>
    public IConnectionMultiplexer? Connection => _connection;

    /// <summary>
    /// Gets the Redis database for direct operations.
    /// </summary>
    /// <returns>The Redis database, or null if not initialized.</returns>
    public IDatabase? GetDatabase() => _connection?.GetDatabase();

    /// <inheritdoc/>
    protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
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

            // Build service provider with lock provider
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddSingleton<IConnectionMultiplexer>(_connection);
            services.Configure<RedisLockOptions>(opt =>
            {
                opt.KeyPrefix = Options.KeyPrefix;
                opt.DefaultExpiry = Options.DefaultExpiry;
                opt.DefaultWait = Options.DefaultWaitTimeout;
                opt.DefaultRetry = Options.DefaultRetryInterval;
            });
            services.AddSingleton<IDistributedLockProvider, RedisDistributedLockProvider>();

            _serviceProvider = services.BuildServiceProvider();
            _containerInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize Redis lock provider: {ex.Message}");
            _containerInitialized = false;
        }
    }

    /// <inheritdoc/>
    public override IDistributedLockProvider CreateLockProvider()
    {
        EnsureInitialized();

        if (_serviceProvider is null)
        {
            throw new InvalidOperationException("Service provider is not available.");
        }

        return _serviceProvider.GetRequiredService<IDistributedLockProvider>();
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
