using LanguageExt;

namespace Encina.Sharding;

/// <summary>
/// A sharded repository that routes operations to the appropriate shard and supports
/// scatter-gather queries across multiple shards.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This interface defines shard-aware data access operations. Single-entity operations
/// are automatically routed to the correct shard based on the entity's shard key.
/// </para>
/// <para>
/// For queries that span multiple shards, use <see cref="QueryAllShardsAsync"/> or
/// <see cref="QueryShardsAsync"/> which execute scatter-gather patterns.
/// </para>
/// <para>
/// Provider-specific implementations (EF Core, Dapper, ADO.NET, MongoDB) will also implement
/// <c>IFunctionalRepository&lt;TEntity, TId&gt;</c> from <c>Encina.DomainModeling</c>,
/// combining standard repository operations with shard routing.
/// </para>
/// <para>
/// All methods return <see cref="Either{EncinaError, T}"/> following the
/// Railway Oriented Programming pattern.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Query all shards for orders by a customer
/// var result = await shardedRepository.QueryAllShardsAsync(
///     async (shardId, ct) =>
///     {
///         var repo = GetRepositoryForShard(shardId);
///         return await repo.ListAsync(new OrdersByCustomerSpec(customerId), ct);
///     },
///     cancellationToken);
///
/// result.Match(
///     Right: queryResult =>
///     {
///         Console.WriteLine($"Found {queryResult.Results.Count} orders across {queryResult.TotalShardsQueried} shards");
///         if (queryResult.IsPartial)
///             Console.WriteLine($"Warning: {queryResult.FailedShards.Count} shards failed");
///     },
///     Left: error => Console.WriteLine($"Error: {error.Message}"));
/// </code>
/// </example>
public interface IFunctionalShardedRepository<TEntity, in TId>
    where TEntity : class
    where TId : notnull
{
    /// <summary>
    /// Gets an entity by its ID from the appropriate shard.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    /// <param name="shardKey">The shard key to determine which shard to query.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Right with the entity if found; Left with an error otherwise.</returns>
    Task<Either<EncinaError, TEntity>> GetByIdAsync(
        TId id,
        string shardKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity to the appropriate shard.
    /// </summary>
    /// <param name="entity">The entity to add. The shard key is extracted automatically.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Right with the added entity; Left with an error if the operation fails.</returns>
    Task<Either<EncinaError, TEntity>> AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity in the appropriate shard.
    /// </summary>
    /// <param name="entity">The entity to update. The shard key is extracted automatically.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Right with the updated entity; Left with an error if the operation fails.</returns>
    Task<Either<EncinaError, TEntity>> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity by its ID from the appropriate shard.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    /// <param name="shardKey">The shard key to determine which shard to query.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Right with Unit on success; Left with an error otherwise.</returns>
    Task<Either<EncinaError, Unit>> DeleteAsync(
        TId id,
        string shardKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries all active shards and aggregates the results.
    /// </summary>
    /// <param name="queryFactory">
    /// A factory that creates a query task for each shard. Receives the shard ID
    /// and a cancellation token, returns the results from that shard.
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a <see cref="ShardedQueryResult{TEntity}"/> containing results from all shards;
    /// Left with an error if the operation fails entirely.
    /// </returns>
    Task<Either<EncinaError, ShardedQueryResult<TEntity>>> QueryAllShardsAsync(
        Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<TEntity>>>> queryFactory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries specific shards and aggregates the results.
    /// </summary>
    /// <param name="shardIds">The shard IDs to query.</param>
    /// <param name="queryFactory">
    /// A factory that creates a query task for each shard. Receives the shard ID
    /// and a cancellation token, returns the results from that shard.
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a <see cref="ShardedQueryResult{TEntity}"/> containing results from the specified shards;
    /// Left with an error if the operation fails entirely.
    /// </returns>
    Task<Either<EncinaError, ShardedQueryResult<TEntity>>> QueryShardsAsync(
        IEnumerable<string> shardIds,
        Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<TEntity>>>> queryFactory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the shard ID that a given entity would be routed to.
    /// </summary>
    /// <param name="entity">The entity to check routing for.</param>
    /// <returns>Right with the shard ID; Left with an error if routing fails.</returns>
    Either<EncinaError, string> GetShardIdForEntity(TEntity entity);
}
