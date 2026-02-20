using System.Data;
using Encina.Messaging.ReadWriteSeparation;
using Encina.Sharding;
using Encina.Sharding.Data;
using Encina.Sharding.ReplicaSelection;
using LanguageExt;
using MySqlConnector;

namespace Encina.Dapper.MySQL.Sharding;

/// <summary>
/// MySQL Dapper implementation of <see cref="IShardedReadWriteConnectionFactory"/> that
/// combines shard routing with read/write separation.
/// </summary>
/// <remarks>
/// <para>
/// Creates <see cref="MySqlConnection"/> instances (returned as <see cref="IDbConnection"/>)
/// routed to the appropriate shard's primary or replica endpoint. Dapper operates on
/// <see cref="IDbConnection"/>, so this factory implements only the non-generic interface.
/// </para>
/// <para>
/// Replica selection is handled by per-shard <see cref="IShardReplicaSelector"/> instances,
/// and unhealthy replicas are automatically excluded via the <see cref="IReplicaHealthTracker"/>.
/// </para>
/// <para>
/// The context-aware <see cref="IShardedReadWriteConnectionFactory.GetConnectionAsync"/>
/// method reads the ambient <see cref="DatabaseRoutingContext"/> to determine whether to
/// connect to the primary or a replica.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Explicit read — always uses a replica
/// var readResult = await factory.GetReadConnectionAsync("shard-0", ct);
/// readResult.Match(
///     Right: conn => conn.QueryAsync&lt;Order&gt;("SELECT * FROM Orders"),
///     Left: error => logger.LogError("Failed: {Error}", error.Message));
///
/// // Explicit write — always uses the primary
/// var writeResult = await factory.GetWriteConnectionAsync("shard-0", ct);
/// </code>
/// </example>
public sealed class ShardedReadWriteConnectionFactory : IShardedReadWriteConnectionFactory
{
    private readonly ShardTopology _topology;
    private readonly ShardedReadWriteOptions _options;
    private readonly IReplicaHealthTracker _healthTracker;
    private readonly Dictionary<string, IShardReplicaSelector> _selectors = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardedReadWriteConnectionFactory"/> class.
    /// </summary>
    /// <param name="topology">The shard topology.</param>
    /// <param name="options">The sharded read/write options.</param>
    /// <param name="healthTracker">The replica health tracker.</param>
    public ShardedReadWriteConnectionFactory(
        ShardTopology topology,
        ShardedReadWriteOptions options,
        IReplicaHealthTracker healthTracker)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(healthTracker);

        _topology = topology;
        _options = options;
        _healthTracker = healthTracker;

        InitializeSelectors();
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IDbConnection>> GetReadConnectionAsync(
        string shardId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);

        var shardResult = _topology.GetShard(shardId);

        return await shardResult
            .MapAsync(async shard =>
            {
                var replicaConnectionString = SelectReplicaConnectionString(shard);
                return await replicaConnectionString
                    .MapAsync(async cs =>
                    {
                        var connection = new MySqlConnection(cs);
                        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                        _healthTracker.MarkHealthy(shardId, cs);
                        return (IDbConnection)connection;
                    })
                    .ConfigureAwait(false);
            })
            .BindAsync(x => x)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IDbConnection>> GetWriteConnectionAsync(
        string shardId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);

        var connectionStringResult = _topology.GetConnectionString(shardId);

        return await connectionStringResult
            .MapAsync(async cs =>
            {
                var connection = new MySqlConnection(cs);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                return (IDbConnection)connection;
            })
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IDbConnection>> GetConnectionAsync(
        string shardId,
        CancellationToken cancellationToken = default)
    {
        return DatabaseRoutingContext.IsReadIntent
            ? await GetReadConnectionAsync(shardId, cancellationToken).ConfigureAwait(false)
            : await GetWriteConnectionAsync(shardId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IReadOnlyDictionary<string, IDbConnection>>>
        GetAllReadConnectionsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAllConnectionsInternalAsync(
            GetReadConnectionAsync, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IReadOnlyDictionary<string, IDbConnection>>>
        GetAllWriteConnectionsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAllConnectionsInternalAsync(
            GetWriteConnectionAsync, cancellationToken).ConfigureAwait(false);
    }

    private Either<EncinaError, string> SelectReplicaConnectionString(ShardInfo shard)
    {
        if (!shard.HasReplicas)
        {
            return _options.FallbackToPrimaryWhenNoReplicas
                ? Either<EncinaError, string>.Right(shard.ConnectionString)
                : Either<EncinaError, string>.Left(
                    EncinaErrors.Create(
                        ShardingErrorCodes.NoReplicasConfigured,
                        $"Shard '{shard.ShardId}' has no read replicas configured."));
        }

        var availableReplicas = _healthTracker.GetAvailableReplicas(
            shard.ShardId, shard.ReplicaConnectionStrings);

        if (availableReplicas.Count == 0)
        {
            return _options.FallbackToPrimaryWhenNoReplicas
                ? Either<EncinaError, string>.Right(shard.ConnectionString)
                : Either<EncinaError, string>.Left(
                    EncinaErrors.Create(
                        ShardingErrorCodes.AllReplicasUnhealthy,
                        $"All replicas for shard '{shard.ShardId}' are currently unhealthy."));
        }

        var selector = _selectors.GetValueOrDefault(shard.ShardId)
            ?? _selectors.GetValueOrDefault(string.Empty)!;

        var selected = selector.SelectReplica(availableReplicas);
        return Either<EncinaError, string>.Right(selected);
    }

    private async Task<Either<EncinaError, IReadOnlyDictionary<string, IDbConnection>>>
        GetAllConnectionsInternalAsync(
        Func<string, CancellationToken, Task<Either<EncinaError, IDbConnection>>> getConnectionAsync,
        CancellationToken cancellationToken)
    {
        var connections = new Dictionary<string, IDbConnection>();

        foreach (var shardId in _topology.ActiveShardIds)
        {
            var result = await getConnectionAsync(shardId, cancellationToken).ConfigureAwait(false);

            var matched = result.Match<Either<EncinaError, IDbConnection>>(
                Right: conn =>
                {
                    connections[shardId] = conn;
                    return Either<EncinaError, IDbConnection>.Right(conn);
                },
                Left: error =>
                {
                    foreach (var conn in connections.Values)
                    {
                        conn.Dispose();
                    }

                    return error;
                });

            if (matched.IsLeft)
            {
                return (EncinaError)matched;
            }
        }

        return Either<EncinaError, IReadOnlyDictionary<string, IDbConnection>>.Right(connections);
    }

    private void InitializeSelectors()
    {
        _selectors[string.Empty] = ShardReplicaSelectorFactory.Create(_options.DefaultReplicaStrategy);

        foreach (var shard in _topology.GetAllShards())
        {
            if (shard.ReplicaStrategy.HasValue)
            {
                _selectors[shard.ShardId] = ShardReplicaSelectorFactory.Create(shard.ReplicaStrategy.Value);
            }
        }
    }
}
