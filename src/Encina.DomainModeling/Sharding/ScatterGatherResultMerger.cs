using System.Linq.Expressions;
using Encina.Sharding;

namespace Encina.DomainModeling.Sharding;

/// <summary>
/// Merges per-shard results from specification-based scatter-gather queries,
/// applying ordering from the specification and optional pagination.
/// </summary>
/// <remarks>
/// <para>
/// This class handles the "gather" phase of scatter-gather queries. After each shard
/// returns its results independently, this merger combines them into a single ordered
/// collection, optionally applying pagination.
/// </para>
/// <para>
/// Ordering is extracted from <see cref="IQuerySpecification{T}"/> to ensure that
/// cross-shard results maintain the same ordering contract as single-shard queries.
/// </para>
/// </remarks>
public static class ScatterGatherResultMerger
{
    /// <summary>
    /// Merges per-shard items into a single ordered list, applying the specification's
    /// ordering expressions.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="perShardItems">The items from each shard, keyed by shard ID.</param>
    /// <param name="specification">
    /// The query specification whose ordering is applied. If the specification has no ordering,
    /// results are returned in shard order (non-deterministic).
    /// </param>
    /// <returns>The merged and ordered list of items.</returns>
    public static IReadOnlyList<T> MergeAndOrder<T>(
        IReadOnlyDictionary<string, IReadOnlyList<T>> perShardItems,
        Specification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(perShardItems);
        ArgumentNullException.ThrowIfNull(specification);

        // Collect all items from all shards
        var totalCount = 0;
        foreach (var items in perShardItems.Values)
        {
            totalCount += items.Count;
        }

        if (totalCount == 0)
        {
            return [];
        }

        var allItems = new List<T>(totalCount);
        foreach (var items in perShardItems.Values)
        {
            allItems.AddRange(items);
        }

        // Apply ordering if the specification has ordering expressions
        if (specification is IQuerySpecification<T> querySpec)
        {
            return ApplyOrdering(allItems, querySpec);
        }

        return allItems;
    }

    /// <summary>
    /// Merges per-shard items and applies pagination on the merged result.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="perShardItems">The items from each shard, keyed by shard ID.</param>
    /// <param name="specification">
    /// The query specification whose ordering is applied before pagination.
    /// </param>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>The paginated subset of the merged, ordered items.</returns>
    public static IReadOnlyList<T> MergeOrderAndPaginate<T>(
        IReadOnlyDictionary<string, IReadOnlyList<T>> perShardItems,
        Specification<T> specification,
        int page,
        int pageSize)
    {
        ArgumentNullException.ThrowIfNull(perShardItems);
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        var ordered = MergeAndOrder(perShardItems, specification);

        var skip = (page - 1) * pageSize;

        if (skip >= ordered.Count)
        {
            return [];
        }

        return ordered
            .Skip(skip)
            .Take(pageSize)
            .ToList();
    }

    /// <summary>
    /// Applies the ordering expressions from a query specification to an in-memory collection.
    /// </summary>
    private static List<T> ApplyOrdering<T>(List<T> items, IQuerySpecification<T> querySpec)
    {
        IOrderedEnumerable<T>? ordered = null;

        // Primary ordering
        if (querySpec.OrderBy is not null)
        {
            var keySelector = querySpec.OrderBy.Compile();
            ordered = items.OrderBy(keySelector);
        }
        else if (querySpec.OrderByDescending is not null)
        {
            var keySelector = querySpec.OrderByDescending.Compile();
            ordered = items.OrderByDescending(keySelector);
        }

        if (ordered is null)
        {
            return items;
        }

        // Secondary ascending ordering
        foreach (var thenByExpr in querySpec.ThenByExpressions)
        {
            var keySelector = thenByExpr.Compile();
            ordered = ordered.ThenBy(keySelector);
        }

        // Secondary descending ordering
        foreach (var thenByDescExpr in querySpec.ThenByDescendingExpressions)
        {
            var keySelector = thenByDescExpr.Compile();
            ordered = ordered.ThenByDescending(keySelector);
        }

        return ordered.ToList();
    }
}
