using System.Data;
using Encina.Messaging.ReadWriteSeparation;
using Encina.Sharding;
using Encina.Sharding.Data;
using Encina.Sharding.ReplicaSelection;
using LanguageExt;
using Microsoft.Data.Sqlite;

namespace Encina.ADO.Sqlite.Sharding;

/// <summary>
/// SQLite implementation of sharded read/write connection factories for ADO.NET.
/// </summary>
/// <remarks>
/// <para>
/// Combines shard routing with read/write separation so that each shard can have a
/// primary (write) endpoint and multiple read replicas. Replica selection is handled
/// by per-shard <see cref="IShardReplicaSelector"/> instances, and unhealthy replicas
/// are automatically excluded via the <see cref="IReplicaHealthTracker"/>.
/// </para>
/// <para>
/// Implements both <see cref="IShardedReadWriteConnectionFactory"/> (returning
/// <see cref="IDbConnection"/>) and <see cref="IShardedReadWriteConnectionFactory{TConnection}"/>
/// (returning <see cref="SqliteConnection"/>) for maximum flexibility.
/// </para>
/// <para>
/// The context-aware <see cref="IShardedReadWriteConnectionFactory{TConnection}.GetConnectionAsync"/>
/// method reads the ambient <see cref="DatabaseRoutingContext"/> to determine whether to
/// connect to the primary or a replica.
/// </para>
/// <para>
/// <b>Note:</b> SQLite does not natively support read replicas. This implementation is
/// provided for API consistency and testing scenarios. In practice, all replicas
/// would point to file-based copies or WAL-mode databases.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Explicit read — always uses a replica
/// var readResult = await factory.GetReadConnectionAsync("shard-0", ct);
///
/// // Explicit write — always uses the primary
/// var writeResult = await factory.GetWriteConnectionAsync("shard-0", ct);
///
/// // Context-aware — uses DatabaseRoutingContext.CurrentIntent
/// DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;
/// var autoResult = await factory.GetConnectionAsync("shard-0", ct);
/// </code>
/// </example>
public sealed class ShardedReadWriteConnectionFactory :
    IShardedReadWriteConnectionFactory,
    IShardedReadWriteConnectionFactory<SqliteConnection>
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
    async Task<Either<EncinaError, SqliteConnection>>
        IShardedReadWriteConnectionFactory<SqliteConnection>.GetReadConnectionAsync(
        string shardId,
        CancellationToken cancellationToken)
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
                        var connection = new SqliteConnection(cs);
                        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                        _healthTracker.MarkHealthy(shardId, cs);
                        return connection;
                    })
                    .ConfigureAwait(false);
            })
            .BindAsync(x => x)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task<Either<EncinaError, SqliteConnection>>
        IShardedReadWriteConnectionFactory<SqliteConnection>.GetWriteConnectionAsync(
        string shardId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);

        var connectionStringResult = _topology.GetConnectionString(shardId);

        return await connectionStringResult
            .MapAsync(async cs =>
            {
                var connection = new SqliteConnection(cs);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                return connection;
            })
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task<Either<EncinaError, SqliteConnection>>
        IShardedReadWriteConnectionFactory<SqliteConnection>.GetConnectionAsync(
        string shardId,
        CancellationToken cancellationToken)
    {
        return DatabaseRoutingContext.IsReadIntent
            ? await ((IShardedReadWriteConnectionFactory<SqliteConnection>)this)
                .GetReadConnectionAsync(shardId, cancellationToken).ConfigureAwait(false)
            : await ((IShardedReadWriteConnectionFactory<SqliteConnection>)this)
                .GetWriteConnectionAsync(shardId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task<Either<EncinaError, IReadOnlyDictionary<string, SqliteConnection>>>
        IShardedReadWriteConnectionFactory<SqliteConnection>.GetAllReadConnectionsAsync(
        CancellationToken cancellationToken)
    {
        return await GetAllConnectionsInternalAsync(
            (shardId, ct) => ((IShardedReadWriteConnectionFactory<SqliteConnection>)this)
                .GetReadConnectionAsync(shardId, ct),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task<Either<EncinaError, IReadOnlyDictionary<string, SqliteConnection>>>
        IShardedReadWriteConnectionFactory<SqliteConnection>.GetAllWriteConnectionsAsync(
        CancellationToken cancellationToken)
    {
        return await GetAllConnectionsInternalAsync(
            (shardId, ct) => ((IShardedReadWriteConnectionFactory<SqliteConnection>)this)
                .GetWriteConnectionAsync(shardId, ct),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IDbConnection>> GetReadConnectionAsync(
        string shardId,
        CancellationToken cancellationToken = default)
    {
        var result = await ((IShardedReadWriteConnectionFactory<SqliteConnection>)this)
            .GetReadConnectionAsync(shardId, cancellationToken)
            .ConfigureAwait(false);

        return result.Map(static conn => (IDbConnection)conn);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IDbConnection>> GetWriteConnectionAsync(
        string shardId,
        CancellationToken cancellationToken = default)
    {
        var result = await ((IShardedReadWriteConnectionFactory<SqliteConnection>)this)
            .GetWriteConnectionAsync(shardId, cancellationToken)
            .ConfigureAwait(false);

        return result.Map(static conn => (IDbConnection)conn);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IDbConnection>> GetConnectionAsync(
        string shardId,
        CancellationToken cancellationToken = default)
    {
        var result = await ((IShardedReadWriteConnectionFactory<SqliteConnection>)this)
            .GetConnectionAsync(shardId, cancellationToken)
            .ConfigureAwait(false);

        return result.Map(static conn => (IDbConnection)conn);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IReadOnlyDictionary<string, IDbConnection>>>
        GetAllReadConnectionsAsync(CancellationToken cancellationToken = default)
    {
        var result = await ((IShardedReadWriteConnectionFactory<SqliteConnection>)this)
            .GetAllReadConnectionsAsync(cancellationToken)
            .ConfigureAwait(false);

        return result.Map(static dict =>
            (IReadOnlyDictionary<string, IDbConnection>)dict
                .ToDictionary(kvp => kvp.Key, kvp => (IDbConnection)kvp.Value));
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IReadOnlyDictionary<string, IDbConnection>>>
        GetAllWriteConnectionsAsync(CancellationToken cancellationToken = default)
    {
        var result = await ((IShardedReadWriteConnectionFactory<SqliteConnection>)this)
            .GetAllWriteConnectionsAsync(cancellationToken)
            .ConfigureAwait(false);

        return result.Map(static dict =>
            (IReadOnlyDictionary<string, IDbConnection>)dict
                .ToDictionary(kvp => kvp.Key, kvp => (IDbConnection)kvp.Value));
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

    private async Task<Either<EncinaError, IReadOnlyDictionary<string, SqliteConnection>>>
        GetAllConnectionsInternalAsync(
        Func<string, CancellationToken, Task<Either<EncinaError, SqliteConnection>>> getConnectionAsync,
        CancellationToken cancellationToken)
    {
        var connections = new Dictionary<string, SqliteConnection>();

        foreach (var shardId in _topology.ActiveShardIds)
        {
            var result = await getConnectionAsync(shardId, cancellationToken).ConfigureAwait(false);

            var matched = result.Match<Either<EncinaError, SqliteConnection>>(
                Right: conn =>
                {
                    connections[shardId] = conn;
                    return conn;
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
                return matched.Match<Either<EncinaError, IReadOnlyDictionary<string, SqliteConnection>>>(
                    Right: _ => throw new InvalidOperationException("Unexpected Right after Left check"),
                    Left: error => error);
            }
        }

        return Either<EncinaError, IReadOnlyDictionary<string, SqliteConnection>>.Right(connections);
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
