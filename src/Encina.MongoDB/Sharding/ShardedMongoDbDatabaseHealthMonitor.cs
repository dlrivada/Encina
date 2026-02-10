using Encina.Database;
using Encina.Sharding;
using Encina.Sharding.Health;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Encina.MongoDB.Sharding;

/// <summary>
/// Health monitor for sharded MongoDB deployments, implementing
/// <see cref="IShardedDatabaseHealthMonitor"/>.
/// </summary>
/// <remarks>
/// <para>
/// This monitor supports both sharding modes:
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Native sharding</b>: Checks health via the <c>mongos</c> client by running
///       a <c>ping</c> command. Detects cluster type (sharded, replica set, standalone)
///       from <see cref="IMongoClient.Cluster"/> description.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Application-level sharding</b>: Checks health of each shard individually
///       by creating connections using the <see cref="ShardTopology"/> connection strings
///       and running <c>ping</c> on each.
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// Pool statistics are derived from <see cref="IMongoClient.Cluster"/> cluster descriptions.
/// MongoDB manages its own internal connection pool per server, but detailed pool metrics
/// are not publicly exposed by the .NET driver.
/// </para>
/// </remarks>
#pragma warning disable CA1848 // Use the LoggerMessage delegates
public sealed class ShardedMongoDbDatabaseHealthMonitor : IShardedDatabaseHealthMonitor
{
    private readonly IMongoClient _defaultClient;
    private readonly ShardTopology _topology;
    private readonly IShardedMongoCollectionFactory _collectionFactory;
    private readonly bool _useNativeSharding;
    private readonly TimeSpan _healthCheckTimeout;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardedMongoDbDatabaseHealthMonitor"/> class.
    /// </summary>
    /// <param name="defaultClient">The default MongoDB client.</param>
    /// <param name="topology">The shard topology.</param>
    /// <param name="collectionFactory">The sharded collection factory.</param>
    /// <param name="useNativeSharding">Whether native mongos sharding is used.</param>
    /// <param name="healthCheckTimeout">Optional timeout for health checks. Defaults to 5 seconds.</param>
    public ShardedMongoDbDatabaseHealthMonitor(
        IMongoClient defaultClient,
        ShardTopology topology,
        IShardedMongoCollectionFactory collectionFactory,
        bool useNativeSharding,
        TimeSpan? healthCheckTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(defaultClient);
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(collectionFactory);

        _defaultClient = defaultClient;
        _topology = topology;
        _collectionFactory = collectionFactory;
        _useNativeSharding = useNativeSharding;
        _healthCheckTimeout = healthCheckTimeout ?? TimeSpan.FromSeconds(5);
    }

    /// <inheritdoc />
    public async Task<ShardHealthResult> CheckShardHealthAsync(
        string shardId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_healthCheckTimeout);

        try
        {
            if (_useNativeSharding)
            {
                // In native mode, check via the default mongos client
                return await PingClientAsync(_defaultClient, shardId, cts.Token).ConfigureAwait(false);
            }

            // Application-level: resolve the shard connection and ping it
            var connectionStringResult = _topology.GetConnectionString(shardId);

            return await connectionStringResult.Match(
                Right: async connectionString =>
                {
                    var client = new MongoClient(connectionString);
                    try
                    {
                        return await PingClientAsync(client, shardId, cts.Token).ConfigureAwait(false);
                    }
                    finally
                    {
                        (client as IDisposable)?.Dispose();
                    }
                },
                Left: _ => Task.FromResult(ShardHealthResult.Unhealthy(
                    shardId,
                    $"Shard '{shardId}' not found in topology."))).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            return ShardHealthResult.Unhealthy(
                shardId,
                $"Health check for shard '{shardId}' timed out after {_healthCheckTimeout.TotalSeconds}s.");
        }
        catch (Exception ex)
        {
            return ShardHealthResult.Unhealthy(shardId, ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public async Task<ShardedHealthSummary> CheckAllShardsHealthAsync(
        CancellationToken cancellationToken = default)
    {
        var shardIds = _useNativeSharding
            ? (IReadOnlyList<string>)["mongos"]
            : _topology.ActiveShardIds;

        var tasks = shardIds.Select(shardId =>
            CheckShardHealthAsync(shardId, cancellationToken));

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        var resultList = results.ToList();

        var overallStatus = ShardedHealthSummary.CalculateOverallStatus(resultList);

        return new ShardedHealthSummary(
            overallStatus,
            resultList,
            $"MongoDB sharded cluster health: {resultList.Count(r => r.IsHealthy)}/{resultList.Count} healthy.");
    }

    /// <inheritdoc />
    public ConnectionPoolStats GetShardPoolStatistics(string shardId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);

        if (_useNativeSharding)
        {
            return GetPoolStatsFromClient(_defaultClient);
        }

        // For application-level sharding, we can only provide stats for the default client
        // since per-shard clients are transient and not cached in the health monitor.
        return ConnectionPoolStats.CreateEmpty();
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, ConnectionPoolStats> GetAllShardPoolStatistics()
    {
        var stats = new Dictionary<string, ConnectionPoolStats>(StringComparer.OrdinalIgnoreCase);

        if (_useNativeSharding)
        {
            stats["mongos"] = GetPoolStatsFromClient(_defaultClient);
        }
        else
        {
            foreach (var shardId in _topology.ActiveShardIds)
            {
                stats[shardId] = ConnectionPoolStats.CreateEmpty();
            }
        }

        return stats;
    }

    private static async Task<ShardHealthResult> PingClientAsync(
        IMongoClient client,
        string shardId,
        CancellationToken cancellationToken)
    {
        var adminDb = client.GetDatabase("admin");
        var pingCommand = new BsonDocument("ping", 1);
        await adminDb.RunCommandAsync<BsonDocument>(pingCommand, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var poolStats = GetPoolStatsFromClient(client);

        return ShardHealthResult.Healthy(
            shardId,
            poolStats,
            $"Shard '{shardId}' is healthy.");
    }

    private static ConnectionPoolStats GetPoolStatsFromClient(IMongoClient client)
    {
        try
        {
            var clusterDescription = client.Cluster.Description;
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
}
#pragma warning restore CA1848
