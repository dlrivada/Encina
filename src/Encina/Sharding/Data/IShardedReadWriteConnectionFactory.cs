using System.Data;
using LanguageExt;

namespace Encina.Sharding.Data;

/// <summary>
/// Factory for creating database connections that combine shard routing with read/write separation.
/// </summary>
/// <remarks>
/// <para>
/// This interface unifies <see cref="IShardedConnectionFactory"/> (shard routing) with
/// read/write separation so that each shard can have a primary (write) endpoint and
/// multiple read replicas. It supports three usage modes:
/// </para>
/// <list type="number">
///   <item><description>
///     <b>Explicit read</b>: <see cref="GetReadConnectionAsync"/> always connects to a replica.
///   </description></item>
///   <item><description>
///     <b>Explicit write</b>: <see cref="GetWriteConnectionAsync"/> always connects to the primary.
///   </description></item>
///   <item><description>
///     <b>Context-aware</b>: <see cref="GetConnectionAsync"/> reads the ambient
///     <c>DatabaseRoutingContext</c> (from <c>Encina.Messaging</c>) to decide automatically.
///   </description></item>
/// </list>
/// <para>
/// When a shard has no replicas configured, read requests fall back to the primary
/// connection string (controlled by <c>ShardedReadWriteOptions.FallbackToPrimaryWhenNoReplicas</c>).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Explicit read — always a replica
/// var readResult = await factory.GetReadConnectionAsync("shard-0", ct);
///
/// // Explicit write — always the primary
/// var writeResult = await factory.GetWriteConnectionAsync("shard-0", ct);
///
/// // Context-aware — uses DatabaseRoutingContext.CurrentIntent
/// DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;
/// var autoResult = await factory.GetConnectionAsync("shard-0", ct);
/// </code>
/// </example>
public interface IShardedReadWriteConnectionFactory
{
    /// <summary>
    /// Gets a read connection to a replica of the specified shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with an open <see cref="IDbConnection"/> to a read replica of the shard;
    /// Left with an error if the shard is not found, has no replicas, or the connection fails.
    /// </returns>
    Task<Either<EncinaError, IDbConnection>> GetReadConnectionAsync(
        string shardId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a write connection to the primary of the specified shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with an open <see cref="IDbConnection"/> to the shard's primary;
    /// Left with an error if the shard is not found or the connection fails.
    /// </returns>
    Task<Either<EncinaError, IDbConnection>> GetWriteConnectionAsync(
        string shardId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a connection to the specified shard, using the ambient <c>DatabaseRoutingContext</c>
    /// to determine whether to connect to the primary or a replica.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with an open <see cref="IDbConnection"/> to the appropriate endpoint;
    /// Left with an error if the shard is not found or the connection fails.
    /// </returns>
    /// <remarks>
    /// When no routing context is set, defaults to the primary (write) connection
    /// for safety.
    /// </remarks>
    Task<Either<EncinaError, IDbConnection>> GetConnectionAsync(
        string shardId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets read connections to replicas of all active shards (scatter-gather reads).
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a dictionary mapping shard IDs to their open read connections;
    /// Left with an error if the operation fails entirely.
    /// </returns>
    Task<Either<EncinaError, IReadOnlyDictionary<string, IDbConnection>>> GetAllReadConnectionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets write connections to the primaries of all active shards (scatter-gather writes).
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a dictionary mapping shard IDs to their open write connections;
    /// Left with an error if the operation fails entirely.
    /// </returns>
    Task<Either<EncinaError, IReadOnlyDictionary<string, IDbConnection>>> GetAllWriteConnectionsAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Factory for creating typed database connections that combine shard routing with read/write separation.
/// </summary>
/// <typeparam name="TConnection">
/// The concrete connection type (e.g., <c>SqlConnection</c>, <c>NpgsqlConnection</c>).
/// </typeparam>
/// <remarks>
/// <para>
/// This generic variant allows provider-specific implementations to return strongly-typed
/// connections while combining shard routing with read/write separation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // SQL Server sharded read/write connection factory
/// IShardedReadWriteConnectionFactory&lt;SqlConnection&gt; factory = ...;
///
/// // Get a typed read connection to a shard's replica
/// var readResult = await factory.GetReadConnectionAsync("shard-0", ct);
/// readResult.Match(
///     Right: conn => { /* conn is SqlConnection */ },
///     Left: error => logger.LogError("Failed: {Error}", error.Message));
/// </code>
/// </example>
public interface IShardedReadWriteConnectionFactory<TConnection>
    where TConnection : class, IDisposable
{
    /// <summary>
    /// Gets a typed read connection to a replica of the specified shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with an open <typeparamref name="TConnection"/> to a read replica;
    /// Left with an error if the shard is not found, has no replicas, or the connection fails.
    /// </returns>
    Task<Either<EncinaError, TConnection>> GetReadConnectionAsync(
        string shardId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a typed write connection to the primary of the specified shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with an open <typeparamref name="TConnection"/> to the shard's primary;
    /// Left with an error if the shard is not found or the connection fails.
    /// </returns>
    Task<Either<EncinaError, TConnection>> GetWriteConnectionAsync(
        string shardId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a typed connection to the specified shard, using the ambient <c>DatabaseRoutingContext</c>
    /// to determine whether to connect to the primary or a replica.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with an open <typeparamref name="TConnection"/> to the appropriate endpoint;
    /// Left with an error if the shard is not found or the connection fails.
    /// </returns>
    Task<Either<EncinaError, TConnection>> GetConnectionAsync(
        string shardId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets typed read connections to replicas of all active shards.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a dictionary mapping shard IDs to their open read connections;
    /// Left with an error if the operation fails entirely.
    /// </returns>
    Task<Either<EncinaError, IReadOnlyDictionary<string, TConnection>>> GetAllReadConnectionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets typed write connections to the primaries of all active shards.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a dictionary mapping shard IDs to their open write connections;
    /// Left with an error if the operation fails entirely.
    /// </returns>
    Task<Either<EncinaError, IReadOnlyDictionary<string, TConnection>>> GetAllWriteConnectionsAsync(
        CancellationToken cancellationToken = default);
}
