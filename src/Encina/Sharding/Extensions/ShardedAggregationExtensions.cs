using System.Linq.Expressions;
using System.Numerics;
using LanguageExt;

namespace Encina.Sharding.Extensions;

/// <summary>
/// Extension methods for distributed aggregation operations on
/// <see cref="IFunctionalShardedRepository{TEntity, TId}"/>.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide a convenient API for performing cross-shard aggregations
/// (Count, Sum, Avg, Min, Max). At runtime, each method checks whether the repository
/// implements <see cref="IShardedAggregationSupport{TEntity, TId}"/>. If it does,
/// the call is delegated to the provider-specific implementation; otherwise a
/// <see cref="NotSupportedException"/> is thrown.
/// </para>
/// <para>
/// Provider implementations (EF Core, Dapper, ADO.NET, MongoDB) should implement
/// <see cref="IShardedAggregationSupport{TEntity, TId}"/> to enable these operations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// IFunctionalShardedRepository&lt;Order, OrderId&gt; repo = ...;
///
/// // Count active orders across all shards
/// var countResult = await repo.CountAcrossShardsAsync(
///     o => o.Status == OrderStatus.Active,
///     cancellationToken);
///
/// // Sum order totals across shards
/// var sumResult = await repo.SumAcrossShardsAsync(
///     o => o.TotalAmount,
///     o => o.Status == OrderStatus.Completed,
///     cancellationToken);
///
/// // Correct global average (two-phase aggregation, avoids average-of-averages)
/// var avgResult = await repo.AvgAcrossShardsAsync(
///     o => o.TotalAmount,
///     cancellationToken: cancellationToken);
///
/// // Min/Max across shards
/// var minResult = await repo.MinAcrossShardsAsync(
///     o => o.TotalAmount,
///     o => o.CreatedAtUtc > cutoff,
///     cancellationToken);
///
/// var maxResult = await repo.MaxAcrossShardsAsync(
///     o => o.TotalAmount,
///     cancellationToken: cancellationToken);
/// </code>
/// </example>
public static class ShardedAggregationExtensions
{
    /// <summary>
    /// Counts entities matching the predicate across all shards.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="repository">The sharded repository.</param>
    /// <param name="predicate">A filter expression to apply on each shard.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with an <see cref="AggregationResult{T}"/> containing the total count;
    /// Left with an error if the operation fails entirely.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the repository does not implement <see cref="IShardedAggregationSupport{TEntity, TId}"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// var result = await repo.CountAcrossShardsAsync(
    ///     o => o.Status == OrderStatus.Active,
    ///     cancellationToken);
    /// </code>
    /// </example>
    public static Task<Either<EncinaError, AggregationResult<long>>> CountAcrossShardsAsync<TEntity, TId>(
        this IFunctionalShardedRepository<TEntity, TId> repository,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(predicate);

        var support = GetAggregationSupport<TEntity, TId>(repository);
        return support.CountAcrossShardsAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Sums a numeric field across all shards for entities matching the predicate.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <typeparam name="TValue">The numeric type of the field to sum.</typeparam>
    /// <param name="repository">The sharded repository.</param>
    /// <param name="selector">An expression selecting the numeric field to sum.</param>
    /// <param name="predicate">An optional filter expression to apply on each shard.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with an <see cref="AggregationResult{T}"/> containing the total sum;
    /// Left with an error if the operation fails entirely.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the repository does not implement <see cref="IShardedAggregationSupport{TEntity, TId}"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// var result = await repo.SumAcrossShardsAsync(
    ///     o => o.TotalAmount,
    ///     o => o.Status == OrderStatus.Completed,
    ///     cancellationToken);
    /// </code>
    /// </example>
    public static Task<Either<EncinaError, AggregationResult<TValue>>> SumAcrossShardsAsync<TEntity, TId, TValue>(
        this IFunctionalShardedRepository<TEntity, TId> repository,
        Expression<Func<TEntity, TValue>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        where TEntity : class
        where TId : notnull
        where TValue : struct, INumber<TValue>
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(selector);

        var support = GetAggregationSupport<TEntity, TId>(repository);
        return support.SumAcrossShardsAsync(selector, predicate, cancellationToken);
    }

    /// <summary>
    /// Computes a correct global average using two-phase aggregation across all shards.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <typeparam name="TValue">The numeric type of the field to average.</typeparam>
    /// <param name="repository">The sharded repository.</param>
    /// <param name="selector">An expression selecting the numeric field to average.</param>
    /// <param name="predicate">An optional filter expression to apply on each shard.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with an <see cref="AggregationResult{T}"/> containing the correct global average;
    /// Left with an error if the operation fails entirely.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the repository does not implement <see cref="IShardedAggregationSupport{TEntity, TId}"/>.
    /// </exception>
    /// <remarks>
    /// This method uses two-phase aggregation: it collects sum and count from each shard,
    /// then computes totalSum / totalCount globally. This avoids the average-of-averages error.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await repo.AvgAcrossShardsAsync(
    ///     o => o.TotalAmount,
    ///     o => o.Status == OrderStatus.Completed,
    ///     cancellationToken);
    /// </code>
    /// </example>
    public static Task<Either<EncinaError, AggregationResult<TValue>>> AvgAcrossShardsAsync<TEntity, TId, TValue>(
        this IFunctionalShardedRepository<TEntity, TId> repository,
        Expression<Func<TEntity, TValue>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        where TEntity : class
        where TId : notnull
        where TValue : struct, INumber<TValue>
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(selector);

        var support = GetAggregationSupport<TEntity, TId>(repository);
        return support.AvgAcrossShardsAsync(selector, predicate, cancellationToken);
    }

    /// <summary>
    /// Finds the minimum value of a field across all shards for entities matching the predicate.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <typeparam name="TValue">The type of the field to find the minimum of.</typeparam>
    /// <param name="repository">The sharded repository.</param>
    /// <param name="selector">An expression selecting the field to find the minimum of.</param>
    /// <param name="predicate">An optional filter expression to apply on each shard.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with an <see cref="AggregationResult{T}"/> containing the global minimum value
    /// (null if no entities matched); Left with an error if the operation fails entirely.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the repository does not implement <see cref="IShardedAggregationSupport{TEntity, TId}"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// var result = await repo.MinAcrossShardsAsync(
    ///     o => o.TotalAmount,
    ///     o => o.CreatedAtUtc > cutoff,
    ///     cancellationToken);
    /// </code>
    /// </example>
    public static Task<Either<EncinaError, AggregationResult<TValue?>>> MinAcrossShardsAsync<TEntity, TId, TValue>(
        this IFunctionalShardedRepository<TEntity, TId> repository,
        Expression<Func<TEntity, TValue>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        where TEntity : class
        where TId : notnull
        where TValue : struct, IComparable<TValue>
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(selector);

        var support = GetAggregationSupport<TEntity, TId>(repository);
        return support.MinAcrossShardsAsync(selector, predicate, cancellationToken);
    }

    /// <summary>
    /// Finds the maximum value of a field across all shards for entities matching the predicate.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <typeparam name="TValue">The type of the field to find the maximum of.</typeparam>
    /// <param name="repository">The sharded repository.</param>
    /// <param name="selector">An expression selecting the field to find the maximum of.</param>
    /// <param name="predicate">An optional filter expression to apply on each shard.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with an <see cref="AggregationResult{T}"/> containing the global maximum value
    /// (null if no entities matched); Left with an error if the operation fails entirely.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the repository does not implement <see cref="IShardedAggregationSupport{TEntity, TId}"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// var result = await repo.MaxAcrossShardsAsync(
    ///     o => o.TotalAmount,
    ///     cancellationToken: cancellationToken);
    /// </code>
    /// </example>
    public static Task<Either<EncinaError, AggregationResult<TValue?>>> MaxAcrossShardsAsync<TEntity, TId, TValue>(
        this IFunctionalShardedRepository<TEntity, TId> repository,
        Expression<Func<TEntity, TValue>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        where TEntity : class
        where TId : notnull
        where TValue : struct, IComparable<TValue>
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(selector);

        var support = GetAggregationSupport<TEntity, TId>(repository);
        return support.MaxAcrossShardsAsync(selector, predicate, cancellationToken);
    }

    private static IShardedAggregationSupport<TEntity, TId> GetAggregationSupport<TEntity, TId>(
        IFunctionalShardedRepository<TEntity, TId> repository)
        where TEntity : class
        where TId : notnull
    {
        if (repository is IShardedAggregationSupport<TEntity, TId> support)
        {
            return support;
        }

        throw new NotSupportedException(
            $"The repository of type '{repository.GetType().Name}' does not implement " +
            $"IShardedAggregationSupport<{typeof(TEntity).Name}, {typeof(TId).Name}>. " +
            $"Use a provider-specific sharded repository that supports distributed aggregation.");
    }
}
