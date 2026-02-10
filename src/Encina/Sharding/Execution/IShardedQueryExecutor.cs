using LanguageExt;

namespace Encina.Sharding.Execution;

/// <summary>
/// Defines the contract for executing scatter-gather queries across multiple shards
/// with parallelism control, timeout management, and partial failure handling.
/// </summary>
/// <remarks>
/// <para>
/// Implementations execute a query factory against one or more shards, aggregating
/// results into a single <see cref="ShardedQueryResult{T}"/>. Failed shards are
/// tracked with their error information in <see cref="ShardFailure"/> records.
/// </para>
/// <para>
/// This interface enables the decorator pattern for cross-cutting concerns such as
/// caching and observability on top of the concrete <see cref="ShardedQueryExecutor"/>.
/// </para>
/// </remarks>
public interface IShardedQueryExecutor
{
    /// <summary>
    /// Executes a query across the specified shards and aggregates the results.
    /// </summary>
    /// <typeparam name="T">The type of the result items.</typeparam>
    /// <param name="shardIds">The shard IDs to query.</param>
    /// <param name="queryFactory">
    /// A factory that creates a query task for each shard. Receives the shard ID
    /// and a cancellation token, returns the results from that shard.
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a <see cref="ShardedQueryResult{T}"/> containing aggregated results;
    /// Left with an error if the operation fails entirely.
    /// </returns>
    Task<Either<EncinaError, ShardedQueryResult<T>>> ExecuteAsync<T>(
        IEnumerable<string> shardIds,
        Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<T>>>> queryFactory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a query across all active shards and aggregates the results.
    /// </summary>
    /// <typeparam name="T">The type of the result items.</typeparam>
    /// <param name="queryFactory">
    /// A factory that creates a query task for each shard.
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a <see cref="ShardedQueryResult{T}"/> containing aggregated results;
    /// Left with an error if the operation fails entirely.
    /// </returns>
    Task<Either<EncinaError, ShardedQueryResult<T>>> ExecuteAllAsync<T>(
        Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<T>>>> queryFactory,
        CancellationToken cancellationToken = default);
}
