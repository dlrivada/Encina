using Encina.Messaging.Health;

namespace Encina.DistributedLock.Redis.Health;

/// <summary>
/// Health check for Redis distributed lock provider.
/// </summary>
public sealed class RedisDistributedLockHealthCheck : IEncinaHealthCheck
{
    private readonly IConnectionMultiplexer _connection;
    private readonly ProviderHealthCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisDistributedLockHealthCheck"/> class.
    /// </summary>
    /// <param name="connection">The Redis connection multiplexer.</param>
    /// <param name="options">The health check options.</param>
    public RedisDistributedLockHealthCheck(
        IConnectionMultiplexer connection,
        ProviderHealthCheckOptions options)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(options);

        _connection = connection;
        _options = options;
    }

    /// <inheritdoc/>
    public string Name => _options.Name ?? "redis-distributed-lock";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> Tags => _options.Tags ?? ["redis", "distributed-lock", "ready"];

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _connection.GetDatabase();
            var testKey = $"health:lock:{Guid.NewGuid():N}";

            // Test SET NX (the core lock operation)
            var setResult = await database.StringSetAsync(
                testKey,
                "health-check",
                TimeSpan.FromSeconds(5),
                When.NotExists).ConfigureAwait(false);

            if (!setResult)
            {
                return HealthCheckResult.Unhealthy("Failed to acquire test lock");
            }

            // Clean up
            await database.KeyDeleteAsync(testKey).ConfigureAwait(false);

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Redis connection failed: {ex.Message}");
        }
    }
}
