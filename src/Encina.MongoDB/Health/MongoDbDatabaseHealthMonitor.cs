using Encina.Database;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Encina.MongoDB.Health;

/// <summary>
/// Database health monitor for MongoDB using the MongoDB .NET Driver.
/// </summary>
/// <remarks>
/// <para>
/// Unlike relational database monitors that inherit from <see cref="Messaging.Health.DatabaseHealthMonitorBase"/>,
/// this implementation directly implements <see cref="IDatabaseHealthMonitor"/> because MongoDB uses
/// <see cref="IMongoClient"/> instead of <see cref="System.Data.IDbConnection"/>.
/// </para>
/// <para>
/// Pool statistics are derived from <see cref="IMongoClient.Cluster"/> cluster description,
/// which provides information about the server topology and connection state.
/// </para>
/// <para>
/// Health checks use the <c>ping</c> command on the <c>admin</c> database, which is the
/// standard MongoDB approach for connectivity verification.
/// </para>
/// </remarks>
public sealed class MongoDbDatabaseHealthMonitor : IDatabaseHealthMonitor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _healthCheckTimeout;
    private volatile bool _isCircuitOpen;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDbDatabaseHealthMonitor"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve IMongoClient from.</param>
    /// <param name="options">Optional resilience configuration.</param>
    public MongoDbDatabaseHealthMonitor(
        IServiceProvider serviceProvider,
        DatabaseResilienceOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _serviceProvider = serviceProvider;
        _healthCheckTimeout = options?.HealthCheckInterval > TimeSpan.Zero
            ? options.HealthCheckInterval
            : TimeSpan.FromSeconds(5);
    }

    /// <inheritdoc />
    public string ProviderName => "mongodb";

    /// <inheritdoc />
    public bool IsCircuitOpen => _isCircuitOpen;

    /// <inheritdoc />
    /// <remarks>
    /// Returns pool statistics estimated from the MongoDB cluster description.
    /// MongoDB manages its own internal connection pool per server, but detailed
    /// pool metrics are not publicly exposed by the .NET driver.
    /// The <see cref="ConnectionPoolStats.ActiveConnections"/> reflects the number of
    /// connected servers in the cluster topology.
    /// </remarks>
    public ConnectionPoolStats GetPoolStatistics()
    {
        try
        {
            var mongoClient = _serviceProvider.GetRequiredService<IMongoClient>();
            var clusterDescription = mongoClient.Cluster.Description;

            var connectedServers = clusterDescription.Servers
                .Count(s => s.State == global::MongoDB.Driver.Core.Servers.ServerState.Connected);
            var totalServers = clusterDescription.Servers.Count;

            return new ConnectionPoolStats(
                ActiveConnections: connectedServers,
                IdleConnections: 0,
                TotalConnections: totalServers,
                PendingRequests: 0,
                MaxPoolSize: totalServers > 0 ? totalServers : 1);
        }
        catch
        {
            return ConnectionPoolStats.CreateEmpty();
        }
    }

    /// <inheritdoc />
    public async Task<DatabaseHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        if (_isCircuitOpen)
        {
            return DatabaseHealthResult.Unhealthy(
                $"Circuit breaker is open for provider '{ProviderName}'.");
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_healthCheckTimeout);

        try
        {
            var mongoClient = _serviceProvider.GetRequiredService<IMongoClient>();

            // Use the standard MongoDB ping command
            var adminDb = mongoClient.GetDatabase("admin");
            var pingCommand = new BsonDocument("ping", 1);
            await adminDb.RunCommandAsync<BsonDocument>(pingCommand, cancellationToken: cts.Token)
                .ConfigureAwait(false);

            _isCircuitOpen = false;

            var stats = GetPoolStatistics();
            var data = new Dictionary<string, object>
            {
                ["provider"] = ProviderName,
                ["activeConnections"] = stats.ActiveConnections,
                ["idleConnections"] = stats.IdleConnections,
                ["totalConnections"] = stats.TotalConnections,
                ["poolUtilization"] = stats.PoolUtilization
            };

            return DatabaseHealthResult.Healthy(
                $"Database connection is healthy for provider '{ProviderName}'.",
                data: data);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            return DatabaseHealthResult.Unhealthy(
                $"Database health check timed out after {_healthCheckTimeout.TotalSeconds}s for provider '{ProviderName}'.");
        }
        catch (Exception ex)
        {
            _isCircuitOpen = true;
            return DatabaseHealthResult.Unhealthy(
                $"Database health check failed for provider '{ProviderName}': {ex.Message}",
                ex);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// MongoDB manages its own internal connection pool. The .NET driver does not expose
    /// a public API to clear the connection pool. This method is a no-op.
    /// </remarks>
    public Task ClearPoolAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
