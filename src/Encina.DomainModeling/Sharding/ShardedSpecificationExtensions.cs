using Encina.Sharding;
using LanguageExt;

namespace Encina.DomainModeling.Sharding;

/// <summary>
/// Extension methods for specification-based scatter-gather operations on
/// <see cref="IFunctionalShardedRepository{TEntity, TId}"/>.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide a convenient API for performing cross-shard queries using
/// <see cref="Specification{T}"/> objects, eliminating the need to manually convert
/// specifications to lambda expressions.
/// </para>
/// <para>
/// At runtime, each method checks whether the repository implements
/// <see cref="IShardedSpecificationSupport{TEntity, TId}"/>. If it does,
/// the call is delegated to the provider-specific implementation; otherwise a
/// <see cref="NotSupportedException"/> is thrown.
/// </para>
/// <para>
/// Provider implementations (EF Core, Dapper, ADO.NET, MongoDB) should implement
/// <see cref="IShardedSpecificationSupport{TEntity, TId}"/> to enable these operations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// IFunctionalShardedRepository&lt;Order, OrderId&gt; repo = ...;
///
/// // Reuse existing specifications across shards
/// var activeOrdersSpec = new ActiveOrdersByRegionSpec("EU");
/// var result = await repo.QueryAllShardsAsync(activeOrdersSpec, ct);
///
/// // Paged scatter-gather
/// var pagedResult = await repo.QueryAllShardsPagedAsync(
///     activeOrdersSpec,
///     new ShardedPaginationOptions { Page = 2, PageSize = 20 },
///     ct);
///
/// // Count across all shards
/// var countResult = await repo.CountAllShardsAsync(activeOrdersSpec, ct);
///
/// // Targeted scatter-gather (specific shards only)
/// var targeted = await repo.QueryShardsAsync(
///     activeOrdersSpec,
///     ["shard-eu-west", "shard-eu-east"],
///     ct);
/// </code>
/// </example>
public static class ShardedSpecificationExtensions
{
    /// <summary>
    /// Queries all active shards using a specification and aggregates the results.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="repository">The sharded repository.</param>
    /// <param name="specification">The specification to evaluate on each shard.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a <see cref="ShardedSpecificationResult{T}"/> containing merged results
    /// with per-shard metadata; Left with an error if the operation fails entirely.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the repository does not implement <see cref="IShardedSpecificationSupport{TEntity, TId}"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// var spec = new ActiveOrdersByRegionSpec("EU");
    /// var result = await repo.QueryAllShardsAsync(spec, ct);
    ///
    /// result.Match(
    ///     Right: r => Console.WriteLine($"Found {r.Items.Count} items across {r.ShardsQueried} shards"),
    ///     Left: error => Console.WriteLine($"Error: {error.Message}"));
    /// </code>
    /// </example>
    public static Task<Either<EncinaError, ShardedSpecificationResult<TEntity>>> QueryAllShardsAsync<TEntity, TId>(
        this IFunctionalShardedRepository<TEntity, TId> repository,
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(specification);

        var support = GetSpecificationSupport<TEntity, TId>(repository);
        return support.QueryAllShardsAsync(specification, cancellationToken);
    }

    /// <summary>
    /// Queries all active shards using a specification with cross-shard pagination.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="repository">The sharded repository.</param>
    /// <param name="specification">The specification to evaluate on each shard.</param>
    /// <param name="pagination">The pagination options including page, size, and merge strategy.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a <see cref="ShardedPagedResult{T}"/> containing the requested page
    /// of merged results; Left with an error if the operation fails entirely.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the repository does not implement <see cref="IShardedSpecificationSupport{TEntity, TId}"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// var spec = new ActiveOrdersByRegionSpec("EU");
    /// var pagination = new ShardedPaginationOptions { Page = 2, PageSize = 20 };
    /// var result = await repo.QueryAllShardsPagedAsync(spec, pagination, ct);
    ///
    /// result.Match(
    ///     Right: r => Console.WriteLine($"Page {r.Page}/{r.TotalPages} ({r.TotalCount} total)"),
    ///     Left: error => Console.WriteLine($"Error: {error.Message}"));
    /// </code>
    /// </example>
    public static Task<Either<EncinaError, ShardedPagedResult<TEntity>>> QueryAllShardsPagedAsync<TEntity, TId>(
        this IFunctionalShardedRepository<TEntity, TId> repository,
        Specification<TEntity> specification,
        ShardedPaginationOptions pagination,
        CancellationToken cancellationToken = default)
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(pagination);

        var support = GetSpecificationSupport<TEntity, TId>(repository);
        return support.QueryAllShardsPagedAsync(specification, pagination, cancellationToken);
    }

    /// <summary>
    /// Counts entities matching the specification across all active shards.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="repository">The sharded repository.</param>
    /// <param name="specification">The specification to evaluate on each shard.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a <see cref="ShardedCountResult"/> containing the total count
    /// with per-shard breakdown; Left with an error if the operation fails entirely.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the repository does not implement <see cref="IShardedSpecificationSupport{TEntity, TId}"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// var spec = new ActiveOrdersByRegionSpec("EU");
    /// var result = await repo.CountAllShardsAsync(spec, ct);
    ///
    /// result.Match(
    ///     Right: r => Console.WriteLine($"Total: {r.TotalCount} across {r.ShardsQueried} shards"),
    ///     Left: error => Console.WriteLine($"Error: {error.Message}"));
    /// </code>
    /// </example>
    public static Task<Either<EncinaError, ShardedCountResult>> CountAllShardsAsync<TEntity, TId>(
        this IFunctionalShardedRepository<TEntity, TId> repository,
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(specification);

        var support = GetSpecificationSupport<TEntity, TId>(repository);
        return support.CountAllShardsAsync(specification, cancellationToken);
    }

    /// <summary>
    /// Queries specific shards using a specification and aggregates the results.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="repository">The sharded repository.</param>
    /// <param name="specification">The specification to evaluate on each shard.</param>
    /// <param name="shardIds">The specific shard IDs to query.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with a <see cref="ShardedSpecificationResult{T}"/> containing merged results
    /// from the specified shards; Left with an error if the operation fails entirely.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the repository does not implement <see cref="IShardedSpecificationSupport{TEntity, TId}"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// var spec = new ActiveOrdersByRegionSpec("EU");
    /// var result = await repo.QueryShardsAsync(spec, ["shard-eu-west", "shard-eu-east"], ct);
    ///
    /// result.Match(
    ///     Right: r => Console.WriteLine($"Found {r.Items.Count} items from {r.ShardsQueried} shards"),
    ///     Left: error => Console.WriteLine($"Error: {error.Message}"));
    /// </code>
    /// </example>
    public static Task<Either<EncinaError, ShardedSpecificationResult<TEntity>>> QueryShardsAsync<TEntity, TId>(
        this IFunctionalShardedRepository<TEntity, TId> repository,
        Specification<TEntity> specification,
        IReadOnlyList<string> shardIds,
        CancellationToken cancellationToken = default)
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(shardIds);

        if (shardIds.Count == 0)
        {
            throw new ArgumentException("At least one shard ID must be specified.", nameof(shardIds));
        }

        var support = GetSpecificationSupport<TEntity, TId>(repository);
        return support.QueryShardsAsync(specification, shardIds, cancellationToken);
    }

    private static IShardedSpecificationSupport<TEntity, TId> GetSpecificationSupport<TEntity, TId>(
        IFunctionalShardedRepository<TEntity, TId> repository)
        where TEntity : class
        where TId : notnull
    {
        if (repository is IShardedSpecificationSupport<TEntity, TId> support)
        {
            return support;
        }

        throw new NotSupportedException(
            $"The repository of type '{repository.GetType().Name}' does not implement " +
            $"IShardedSpecificationSupport<{typeof(TEntity).Name}, {typeof(TId).Name}>. " +
            $"Use a provider-specific sharded repository that supports specification-based scatter-gather.");
    }
}
