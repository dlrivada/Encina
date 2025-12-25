using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Encina.Caching.Redis.Health;

/// <summary>
/// Health check for Redis cache connectivity.
/// </summary>
/// <remarks>
/// This health check is compatible with Redis and all Redis-compatible servers
/// including Valkey, KeyDB, Dragonfly, and Garnet.
/// </remarks>
public sealed class RedisHealthCheck : EncinaHealthCheck
{
    /// <summary>
    /// The default name for the Redis health check.
    /// </summary>
    public const string DefaultName = "encina-redis";

    private readonly IServiceProvider _serviceProvider;
    private readonly ProviderHealthCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve IConnectionMultiplexer from.</param>
    /// <param name="options">Configuration for the health check. If null, default options are used.</param>
    public RedisHealthCheck(
        IServiceProvider serviceProvider,
        ProviderHealthCheckOptions? options)
        : base(options?.Name ?? DefaultName, options?.Tags ?? ["encina", "cache", "redis", "ready"])
    {
        _serviceProvider = serviceProvider;
        _options = options ?? new ProviderHealthCheckOptions();
    }

    /// <inheritdoc/>
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(_options.Timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            var connection = _serviceProvider.GetRequiredService<IConnectionMultiplexer>();

            if (!connection.IsConnected)
            {
                return HealthCheckResult.Unhealthy($"{Name} is not connected");
            }

            // Ping all endpoints
            var endpoints = connection.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = connection.GetServer(endpoint);
                var pingTime = await server.PingAsync().ConfigureAwait(false);

                // Check if ping took too long (more than half the timeout)
                if (pingTime > _options.Timeout / 2)
                {
                    return HealthCheckResult.Degraded(
                        $"{Name} is slow (ping: {pingTime.TotalMilliseconds}ms)",
                        data: new Dictionary<string, object> { ["ping_ms"] = pingTime.TotalMilliseconds });
                }
            }

            return HealthCheckResult.Healthy($"{Name} is reachable");
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy($"{Name} health check timed out after {_options.Timeout.TotalSeconds}s");
        }
        catch (RedisException ex)
        {
            return HealthCheckResult.Unhealthy($"{Name} health check failed: {ex.Message}");
        }
    }
}
