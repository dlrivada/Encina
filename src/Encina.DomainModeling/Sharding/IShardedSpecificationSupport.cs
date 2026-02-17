using Encina.Sharding;
using LanguageExt;

namespace Encina.DomainModeling.Sharding;

/// <summary>
/// Provides specification-based scatter-gather query operations across shards.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// Implementations of this interface evaluate <see cref="Specification{T}"/> objects
/// against individual shards using the provider's native mechanism (IQueryable for EF Core,
/// parameterized SQL for Dapper/ADO.NET, FilterDefinition for MongoDB) and merge the results.
/// </para>
/// <para>
/// Provider-specific implementations (EF Core, Dapper, ADO.NET, MongoDB) should implement this
/// interface alongside <see cref="IFunctionalShardedRepository{TEntity, TId}"/> to enable
/// specification-based scatter-gather support.
/// </para>
/// <para>
/// When a repository does not implement this interface, the extension methods in
/// <see cref="ShardedSpecificationExtensions"/> will throw <see cref="NotSupportedException"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Specification-based scatter-gather
/// var spec = new ActiveOrdersByRegionSpec("EU");
/// var result = await repo.QueryAllShardsAsync(spec, ct);
///
/// // Paged scatter-gather
/// var pagedResult = await repo.QueryAllShardsPagedAsync(
///     spec,
///     new ShardedPaginationOptions { Page = 2, PageSize = 20 },
///     ct);
///
/// // Count across shards
/// var countResult = await repo.CountAllShardsAsync(spec, ct);
/// </code>
/// </example>
public interface IShardedSpecificationSupport<TEntity, in TId>
    where TEntity : class
    where TId : notnull
{
    /// <summary>
    /// Queries all active shards using a specification and aggregates the results.
    /// </summary>
    /// <param name="specification">The specification to evaluate on each shard.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a <see cref="ShardedSpecificationResult{T}"/> containing merged results
    /// with per-shard metadata; Left with an error if the operation fails entirely.
    /// </returns>
    Task<Either<EncinaError, ShardedSpecificationResult<TEntity>>> QueryAllShardsAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries all active shards using a specification with cross-shard pagination.
    /// </summary>
    /// <param name="specification">The specification to evaluate on each shard.</param>
    /// <param name="pagination">The pagination options including page, size, and merge strategy.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a <see cref="ShardedPagedResult{T}"/> containing the requested page
    /// of merged results; Left with an error if the operation fails entirely.
    /// </returns>
    Task<Either<EncinaError, ShardedPagedResult<TEntity>>> QueryAllShardsPagedAsync(
        Specification<TEntity> specification,
        ShardedPaginationOptions pagination,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities matching the specification across all active shards.
    /// </summary>
    /// <param name="specification">The specification to evaluate on each shard.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a <see cref="ShardedCountResult"/> containing the total count
    /// with per-shard breakdown; Left with an error if the operation fails entirely.
    /// </returns>
    Task<Either<EncinaError, ShardedCountResult>> CountAllShardsAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries specific shards using a specification and aggregates the results.
    /// </summary>
    /// <param name="specification">The specification to evaluate on each shard.</param>
    /// <param name="shardIds">The specific shard IDs to query.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a <see cref="ShardedSpecificationResult{T}"/> containing merged results
    /// from the specified shards; Left with an error if the operation fails entirely.
    /// </returns>
    Task<Either<EncinaError, ShardedSpecificationResult<TEntity>>> QueryShardsAsync(
        Specification<TEntity> specification,
        IReadOnlyList<string> shardIds,
        CancellationToken cancellationToken = default);
}
