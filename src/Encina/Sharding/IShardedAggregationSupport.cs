using System.Linq.Expressions;
using System.Numerics;
using LanguageExt;

namespace Encina.Sharding;

/// <summary>
/// Provides distributed aggregation operations across shards using two-phase aggregation.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// Implementations of this interface perform aggregations by querying each shard individually,
/// collecting partial results, and then combining them using mathematically correct two-phase
/// aggregation. This avoids common pitfalls like average-of-averages errors.
/// </para>
/// <para>
/// Provider-specific implementations (EF Core, Dapper, ADO.NET, MongoDB) should implement this
/// interface alongside <see cref="IFunctionalShardedRepository{TEntity, TId}"/> to enable
/// distributed aggregation support.
/// </para>
/// <para>
/// When a repository does not implement this interface, the extension methods in
/// <see cref="Extensions.ShardedAggregationExtensions"/> will throw <see cref="NotSupportedException"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Count all active orders across shards
/// var countResult = await aggregationSupport.CountAcrossShardsAsync(
///     order => order.Status == OrderStatus.Active,
///     cancellationToken);
///
/// countResult.Match(
///     Right: result => Console.WriteLine($"Found {result.Value} active orders across {result.ShardsQueried} shards"),
///     Left: error => Console.WriteLine($"Error: {error.Message}"));
///
/// // Average order amount with correct two-phase aggregation
/// var avgResult = await aggregationSupport.AvgAcrossShardsAsync(
///     order => order.TotalAmount,
///     order => order.Status == OrderStatus.Completed,
///     cancellationToken);
/// </code>
/// </example>
public interface IShardedAggregationSupport<TEntity, in TId>
    where TEntity : class
    where TId : notnull
{
    /// <summary>
    /// Counts entities matching the predicate across all shards.
    /// </summary>
    /// <param name="predicate">A filter expression to apply on each shard.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with an <see cref="AggregationResult{T}"/> containing the total count;
    /// Left with an error if the operation fails entirely.
    /// </returns>
    Task<Either<EncinaError, AggregationResult<long>>> CountAcrossShardsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sums a numeric field across all shards for entities matching the predicate.
    /// </summary>
    /// <typeparam name="TValue">The numeric type of the field to sum.</typeparam>
    /// <param name="selector">An expression selecting the numeric field to sum.</param>
    /// <param name="predicate">An optional filter expression to apply on each shard.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with an <see cref="AggregationResult{T}"/> containing the total sum;
    /// Left with an error if the operation fails entirely.
    /// </returns>
    Task<Either<EncinaError, AggregationResult<TValue>>> SumAcrossShardsAsync<TValue>(
        Expression<Func<TEntity, TValue>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        where TValue : struct, INumber<TValue>;

    /// <summary>
    /// Computes a correct global average using two-phase aggregation across all shards.
    /// </summary>
    /// <typeparam name="TValue">The numeric type of the field to average.</typeparam>
    /// <param name="selector">An expression selecting the numeric field to average.</param>
    /// <param name="predicate">An optional filter expression to apply on each shard.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with an <see cref="AggregationResult{T}"/> containing the correct global average;
    /// Left with an error if the operation fails entirely.
    /// </returns>
    /// <remarks>
    /// The average is computed by summing all per-shard sums and all per-shard counts,
    /// then dividing totalSum / totalCount. This avoids the average-of-averages error
    /// that occurs when naively averaging per-shard averages.
    /// </remarks>
    Task<Either<EncinaError, AggregationResult<TValue>>> AvgAcrossShardsAsync<TValue>(
        Expression<Func<TEntity, TValue>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        where TValue : struct, INumber<TValue>;

    /// <summary>
    /// Finds the minimum value of a field across all shards for entities matching the predicate.
    /// </summary>
    /// <typeparam name="TValue">The type of the field to find the minimum of.</typeparam>
    /// <param name="selector">An expression selecting the field to find the minimum of.</param>
    /// <param name="predicate">An optional filter expression to apply on each shard.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with an <see cref="AggregationResult{T}"/> containing the global minimum value;
    /// Left with an error if the operation fails entirely.
    /// </returns>
    Task<Either<EncinaError, AggregationResult<TValue?>>> MinAcrossShardsAsync<TValue>(
        Expression<Func<TEntity, TValue>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        where TValue : struct, IComparable<TValue>;

    /// <summary>
    /// Finds the maximum value of a field across all shards for entities matching the predicate.
    /// </summary>
    /// <typeparam name="TValue">The type of the field to find the maximum of.</typeparam>
    /// <param name="selector">An expression selecting the field to find the maximum of.</param>
    /// <param name="predicate">An optional filter expression to apply on each shard.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// Right with an <see cref="AggregationResult{T}"/> containing the global maximum value;
    /// Left with an error if the operation fails entirely.
    /// </returns>
    Task<Either<EncinaError, AggregationResult<TValue?>>> MaxAcrossShardsAsync<TValue>(
        Expression<Func<TEntity, TValue>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        where TValue : struct, IComparable<TValue>;
}
