using LanguageExt;

namespace Encina.Sharding.Data;

/// <summary>
/// Factory for creating <c>DbContext</c> instances connected to specific shards.
/// </summary>
/// <typeparam name="TContext">The DbContext type. Must derive from <c>Microsoft.EntityFrameworkCore.DbContext</c>.</typeparam>
/// <remarks>
/// <para>
/// This interface provides the EF Core integration point for sharded database access.
/// It creates DbContext instances with shard-specific connection strings, following
/// the same pattern as <c>IReadWriteDbContextFactory&lt;TContext&gt;</c> from the
/// read/write separation module.
/// </para>
/// <para>
/// The type parameter is constrained to <c>class, IDisposable</c> rather than
/// <c>DbContext</c> directly because the core Encina package does not reference
/// Entity Framework Core. The actual EF Core constraint is enforced by provider
/// implementations in <c>Encina.EntityFrameworkCore</c>.
/// </para>
/// <para>
/// All methods return <see cref="Either{EncinaError, T}"/> to gracefully handle
/// shard unavailability without throwing exceptions.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a context for a specific shard
/// var result = shardedFactory.CreateContextForShard("shard-0");
/// result.Match(
///     Right: context => { /* use context */ },
///     Left: error => logger.LogError("Failed: {Error}", error.Message));
///
/// // Route an entity to its shard and create context
/// var ctxResult = shardedFactory.CreateContextForEntity(order);
/// </code>
/// </example>
public interface IShardedDbContextFactory<TContext>
    where TContext : class, IDisposable
{
    /// <summary>
    /// Creates a DbContext connected to the specified shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <returns>
    /// Right with a new <typeparamref name="TContext"/> instance connected to the shard;
    /// Left with an error if the shard is not found.
    /// </returns>
    Either<EncinaError, TContext> CreateContextForShard(string shardId);

    /// <summary>
    /// Creates a DbContext connected to the shard that an entity routes to.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entity">The entity to route.</param>
    /// <returns>
    /// Right with a new <typeparamref name="TContext"/> instance connected to the entity's shard;
    /// Left with an error if routing fails or the shard is not found.
    /// </returns>
    Either<EncinaError, TContext> CreateContextForEntity<TEntity>(TEntity entity)
        where TEntity : notnull;

    /// <summary>
    /// Creates DbContext instances for all active shards.
    /// </summary>
    /// <returns>
    /// Right with a dictionary mapping shard IDs to their DbContext instances;
    /// Left with an error if the operation fails.
    /// </returns>
    /// <remarks>
    /// The caller is responsible for disposing all returned contexts.
    /// Only active shards from the topology are included.
    /// </remarks>
    Either<EncinaError, IReadOnlyDictionary<string, TContext>> CreateAllContexts();

    /// <summary>
    /// Asynchronously creates a DbContext connected to the specified shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a new <typeparamref name="TContext"/> instance connected to the shard;
    /// Left with an error if the shard is not found or connection fails.
    /// </returns>
    ValueTask<Either<EncinaError, TContext>> CreateContextForShardAsync(
        string shardId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously creates a DbContext connected to the shard that an entity routes to.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entity">The entity to route.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a new <typeparamref name="TContext"/> instance connected to the entity's shard;
    /// Left with an error if routing fails or the shard is not found.
    /// </returns>
    ValueTask<Either<EncinaError, TContext>> CreateContextForEntityAsync<TEntity>(
        TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : notnull;

    /// <summary>
    /// Asynchronously creates DbContext instances for all active shards.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a dictionary mapping shard IDs to their DbContext instances;
    /// Left with an error if the operation fails.
    /// </returns>
    ValueTask<Either<EncinaError, IReadOnlyDictionary<string, TContext>>> CreateAllContextsAsync(
        CancellationToken cancellationToken = default);
}
