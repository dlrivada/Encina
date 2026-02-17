using System.Data;
using Encina.Sharding;
using Encina.Sharding.Data;
using LanguageExt;
using Microsoft.Data.SqlClient;

namespace Encina.ADO.SqlServer.Sharding;

/// <summary>
/// SQL Server implementation of sharded connection factories for ADO.NET.
/// </summary>
/// <remarks>
/// <para>
/// Creates <see cref="SqlConnection"/> instances routed to the appropriate shard
/// based on the <see cref="ShardTopology"/> and <see cref="IShardRouter"/>.
/// </para>
/// <para>
/// Implements both <see cref="IShardedConnectionFactory"/> (returning <see cref="IDbConnection"/>)
/// and <see cref="IShardedConnectionFactory{TConnection}"/> (returning <see cref="SqlConnection"/>)
/// for maximum flexibility.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Get a typed connection to a specific shard
/// var factory = serviceProvider.GetRequiredService&lt;IShardedConnectionFactory&lt;SqlConnection&gt;&gt;();
/// var result = await factory.GetConnectionAsync("shard-0", ct);
/// result.Match(
///     Right: conn => { /* use SqlConnection */ },
///     Left: error => logger.LogError("Failed: {Error}", error.Message));
/// </code>
/// </example>
public sealed class ShardedConnectionFactory :
    IShardedConnectionFactory,
    IShardedConnectionFactory<SqlConnection>
{
    private readonly ShardTopology _topology;
    private readonly IShardRouter _router;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardedConnectionFactory"/> class.
    /// </summary>
    /// <param name="topology">The shard topology.</param>
    /// <param name="router">The shard router.</param>
    public ShardedConnectionFactory(
        ShardTopology topology,
        IShardRouter router)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(router);

        _topology = topology;
        _router = router;
    }

    /// <inheritdoc />
    async Task<Either<EncinaError, SqlConnection>> IShardedConnectionFactory<SqlConnection>.GetConnectionAsync(
        string shardId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);

        var connectionStringResult = _topology.GetConnectionString(shardId);

        return await connectionStringResult
            .MapAsync(async cs =>
            {
                var connection = new SqlConnection(cs);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                return connection;
            })
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task<Either<EncinaError, IReadOnlyDictionary<string, SqlConnection>>>
        IShardedConnectionFactory<SqlConnection>.GetAllConnectionsAsync(
        CancellationToken cancellationToken)
    {
        var connections = new Dictionary<string, SqlConnection>();

        foreach (var shardId in _topology.ActiveShardIds)
        {
            var result = await ((IShardedConnectionFactory<SqlConnection>)this)
                .GetConnectionAsync(shardId, cancellationToken)
                .ConfigureAwait(false);

            var matched = result.Match<Either<EncinaError, SqlConnection>>(
                Right: conn =>
                {
                    connections[shardId] = conn;
                    return conn;
                },
                Left: error =>
                {
                    // Cleanup already-opened connections on failure
                    foreach (var conn in connections.Values)
                    {
                        conn.Dispose();
                    }

                    return error;
                });

            if (matched.IsLeft)
            {
                return matched.Match<Either<EncinaError, IReadOnlyDictionary<string, SqlConnection>>>(
                    Right: _ => throw new InvalidOperationException("Unexpected Right after Left check"),
                    Left: error => error);
            }
        }

        return Either<EncinaError, IReadOnlyDictionary<string, SqlConnection>>
            .Right(connections);
    }

    /// <inheritdoc />
    async Task<Either<EncinaError, SqlConnection>>
        IShardedConnectionFactory<SqlConnection>.GetConnectionForEntityAsync<TEntity>(
        TEntity entity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var shardKeyResult = ShardKeyExtractor.Extract(entity);

        return await shardKeyResult
            .Bind(key => _router.GetShardId(key))
            .MapAsync(async shardId =>
                await ((IShardedConnectionFactory<SqlConnection>)this)
                    .GetConnectionAsync(shardId, cancellationToken)
                    .ConfigureAwait(false))
            .BindAsync(x => x)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IDbConnection>> GetConnectionAsync(
        string shardId,
        CancellationToken cancellationToken = default)
    {
        var result = await ((IShardedConnectionFactory<SqlConnection>)this)
            .GetConnectionAsync(shardId, cancellationToken)
            .ConfigureAwait(false);

        return result.Map(static conn => (IDbConnection)conn);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IReadOnlyDictionary<string, IDbConnection>>> GetAllConnectionsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await ((IShardedConnectionFactory<SqlConnection>)this)
            .GetAllConnectionsAsync(cancellationToken)
            .ConfigureAwait(false);

        return result.Map(static dict =>
            (IReadOnlyDictionary<string, IDbConnection>)dict
                .ToDictionary(kvp => kvp.Key, kvp => (IDbConnection)kvp.Value));
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IDbConnection>> GetConnectionForEntityAsync<TEntity>(
        TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : notnull
    {
        var result = await ((IShardedConnectionFactory<SqlConnection>)this)
            .GetConnectionForEntityAsync(entity, cancellationToken)
            .ConfigureAwait(false);

        return result.Map(static conn => (IDbConnection)conn);
    }
}
