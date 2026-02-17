using LanguageExt;

namespace Encina.Sharding.Data;

/// <summary>
/// Factory for creating <c>DbContext</c> instances that combine shard routing with read/write separation.
/// </summary>
/// <typeparam name="TContext">The DbContext type. Must derive from <c>Microsoft.EntityFrameworkCore.DbContext</c>.</typeparam>
/// <remarks>
/// <para>
/// This interface unifies <see cref="IShardedDbContextFactory{TContext}"/> (shard routing)
/// with read/write separation so that each shard can have a primary (write) DbContext
/// and read replica DbContext instances. It supports three usage modes:
/// </para>
/// <list type="number">
///   <item><description>
///     <b>Explicit read</b>: <see cref="CreateReadContextForShard"/> always creates a context connected to a replica.
///   </description></item>
///   <item><description>
///     <b>Explicit write</b>: <see cref="CreateWriteContextForShard"/> always creates a context connected to the primary.
///   </description></item>
///   <item><description>
///     <b>Context-aware</b>: <see cref="CreateContextForShard"/> reads the ambient
///     <c>DatabaseRoutingContext</c> (from <c>Encina.Messaging</c>) to decide automatically.
///   </description></item>
/// </list>
/// <para>
/// The type parameter is constrained to <c>class, IDisposable</c> rather than <c>DbContext</c>
/// directly because the core Encina package does not reference Entity Framework Core.
/// The actual EF Core constraint is enforced by provider implementations.
/// </para>
/// <para>
/// When a shard has no replicas configured, read requests fall back to the primary
/// connection string (controlled by <c>ShardedReadWriteOptions.FallbackToPrimaryWhenNoReplicas</c>).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Explicit read — always a replica context
/// var readResult = factory.CreateReadContextForShard("shard-0");
///
/// // Explicit write — always the primary context
/// var writeResult = factory.CreateWriteContextForShard("shard-0");
///
/// // Context-aware — uses DatabaseRoutingContext.CurrentIntent
/// DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;
/// var autoResult = factory.CreateContextForShard("shard-0");
/// </code>
/// </example>
public interface IShardedReadWriteDbContextFactory<TContext>
    where TContext : class, IDisposable
{
    /// <summary>
    /// Creates a DbContext connected to a read replica of the specified shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <returns>
    /// Right with a new <typeparamref name="TContext"/> instance connected to a read replica;
    /// Left with an error if the shard is not found, has no replicas, or the connection fails.
    /// </returns>
    Either<EncinaError, TContext> CreateReadContextForShard(string shardId);

    /// <summary>
    /// Creates a DbContext connected to the primary of the specified shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <returns>
    /// Right with a new <typeparamref name="TContext"/> instance connected to the shard's primary;
    /// Left with an error if the shard is not found.
    /// </returns>
    Either<EncinaError, TContext> CreateWriteContextForShard(string shardId);

    /// <summary>
    /// Creates a DbContext for the specified shard, using the ambient <c>DatabaseRoutingContext</c>
    /// to determine whether to connect to the primary or a replica.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <returns>
    /// Right with a new <typeparamref name="TContext"/> instance;
    /// Left with an error if the shard is not found or the connection fails.
    /// </returns>
    /// <remarks>
    /// When no routing context is set, defaults to the primary (write) connection for safety.
    /// </remarks>
    Either<EncinaError, TContext> CreateContextForShard(string shardId);

    /// <summary>
    /// Creates read replica DbContext instances for all active shards (scatter-gather reads).
    /// </summary>
    /// <returns>
    /// Right with a dictionary mapping shard IDs to their read replica contexts;
    /// Left with an error if the operation fails entirely.
    /// </returns>
    Either<EncinaError, IReadOnlyDictionary<string, TContext>> CreateAllReadContexts();

    /// <summary>
    /// Creates primary DbContext instances for all active shards (scatter-gather writes).
    /// </summary>
    /// <returns>
    /// Right with a dictionary mapping shard IDs to their primary contexts;
    /// Left with an error if the operation fails entirely.
    /// </returns>
    Either<EncinaError, IReadOnlyDictionary<string, TContext>> CreateAllWriteContexts();

    /// <summary>
    /// Asynchronously creates a DbContext connected to a read replica of the specified shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a new <typeparamref name="TContext"/> instance connected to a read replica;
    /// Left with an error if the shard is not found, has no replicas, or the connection fails.
    /// </returns>
    ValueTask<Either<EncinaError, TContext>> CreateReadContextForShardAsync(
        string shardId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously creates a DbContext connected to the primary of the specified shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a new <typeparamref name="TContext"/> instance connected to the shard's primary;
    /// Left with an error if the shard is not found.
    /// </returns>
    ValueTask<Either<EncinaError, TContext>> CreateWriteContextForShardAsync(
        string shardId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously creates a DbContext for the specified shard, using the ambient routing context.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a new <typeparamref name="TContext"/> instance;
    /// Left with an error if the shard is not found or the connection fails.
    /// </returns>
    ValueTask<Either<EncinaError, TContext>> CreateContextForShardAsync(
        string shardId,
        CancellationToken cancellationToken = default);
}
