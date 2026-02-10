using System.Data;
using LanguageExt;

namespace Encina.Sharding.Data;

/// <summary>
/// Factory for creating database connections routed to specific shards.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides the core abstraction for obtaining database connections
/// in a sharded architecture. It integrates with <see cref="IShardRouter"/> to
/// automatically route entities to the correct shard, and with <see cref="ShardTopology"/>
/// to resolve connection strings.
/// </para>
/// <para>
/// Provider-specific implementations (ADO.NET, Dapper) should implement the generic
/// variant <see cref="IShardedConnectionFactory{TConnection}"/> for typed connections.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Get a connection to a specific shard
/// var result = await connectionFactory.GetConnectionAsync("shard-0", cancellationToken);
/// result.Match(
///     Right: connection => { /* use connection */ },
///     Left: error => logger.LogError("Shard unavailable: {Error}", error.Message));
///
/// // Route an entity to its shard automatically
/// var connResult = await connectionFactory.GetConnectionForEntityAsync(order, cancellationToken);
/// </code>
/// </example>
public interface IShardedConnectionFactory
{
    /// <summary>
    /// Gets a database connection to the specified shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with an open <see cref="IDbConnection"/> to the shard;
    /// Left with an error if the shard is not found or the connection fails.
    /// </returns>
    /// <remarks>
    /// The caller is responsible for disposing the returned connection.
    /// </remarks>
    Task<Either<EncinaError, IDbConnection>> GetConnectionAsync(
        string shardId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets database connections to all active shards.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a dictionary mapping shard IDs to their open connections;
    /// Left with an error if the operation fails entirely.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Only active shards from the topology are included. The caller is responsible
    /// for disposing all returned connections.
    /// </para>
    /// </remarks>
    Task<Either<EncinaError, IReadOnlyDictionary<string, IDbConnection>>> GetAllConnectionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a database connection for the shard that an entity routes to.
    /// </summary>
    /// <typeparam name="TEntity">The entity type. Must implement <see cref="IShardable"/> or
    /// have a property decorated with <see cref="ShardKeyAttribute"/>.</typeparam>
    /// <param name="entity">The entity to route.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with an open <see cref="IDbConnection"/> to the entity's shard;
    /// Left with an error if routing or connection fails.
    /// </returns>
    Task<Either<EncinaError, IDbConnection>> GetConnectionForEntityAsync<TEntity>(
        TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : notnull;
}

/// <summary>
/// Factory for creating typed database connections routed to specific shards.
/// </summary>
/// <typeparam name="TConnection">
/// The concrete connection type (e.g., <c>SqlConnection</c>, <c>NpgsqlConnection</c>).
/// </typeparam>
/// <remarks>
/// <para>
/// This generic variant allows provider-specific implementations to return strongly-typed
/// connections. It follows the same pattern as <c>ITenantConnectionFactory&lt;TConnection&gt;</c>
/// from the multi-tenancy module.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // SQL Server sharded connection factory
/// public class SqlServerShardedConnectionFactory : IShardedConnectionFactory&lt;SqlConnection&gt;
/// {
///     public async Task&lt;Either&lt;EncinaError, SqlConnection&gt;&gt; GetConnectionAsync(
///         string shardId, CancellationToken ct)
///     {
///         var connStr = _topology.GetConnectionString(shardId);
///         return await connStr.MapAsync(async cs =>
///         {
///             var conn = new SqlConnection(cs);
///             await conn.OpenAsync(ct);
///             return conn;
///         });
///     }
/// }
/// </code>
/// </example>
public interface IShardedConnectionFactory<TConnection>
    where TConnection : class, IDisposable
{
    /// <summary>
    /// Gets a typed database connection to the specified shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with an open <typeparamref name="TConnection"/> to the shard;
    /// Left with an error if the shard is not found or the connection fails.
    /// </returns>
    Task<Either<EncinaError, TConnection>> GetConnectionAsync(
        string shardId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets typed database connections to all active shards.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a dictionary mapping shard IDs to their open connections;
    /// Left with an error if the operation fails entirely.
    /// </returns>
    Task<Either<EncinaError, IReadOnlyDictionary<string, TConnection>>> GetAllConnectionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a typed database connection for the shard that an entity routes to.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entity">The entity to route.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with an open <typeparamref name="TConnection"/> to the entity's shard;
    /// Left with an error if routing or connection fails.
    /// </returns>
    Task<Either<EncinaError, TConnection>> GetConnectionForEntityAsync<TEntity>(
        TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : notnull;
}
