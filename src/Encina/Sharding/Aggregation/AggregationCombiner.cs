using System.Numerics;

namespace Encina.Sharding.Aggregation;

/// <summary>
/// Combines partial aggregation results from multiple shards into a single global result
/// using mathematically correct two-phase aggregation.
/// </summary>
/// <remarks>
/// <para>
/// This class implements the "combine" phase of two-phase distributed aggregation.
/// Each shard independently computes its local aggregates (phase 1), and this combiner
/// merges them into a global result (phase 2).
/// </para>
/// <para>
/// The key insight is that some aggregations (like average) cannot be combined by
/// simply averaging the per-shard results. Instead, the combiner uses the raw sum
/// and count from each shard to compute totalSum / totalCount.
/// </para>
/// </remarks>
public static class AggregationCombiner
{
    /// <summary>
    /// Combines per-shard counts into a global count.
    /// </summary>
    /// <typeparam name="TValue">The numeric type of the partial values.</typeparam>
    /// <param name="partials">The partial results from each shard.</param>
    /// <returns>The total count across all shards, or zero if no partials.</returns>
    public static long CombineCount<TValue>(IReadOnlyList<ShardAggregatePartial<TValue>> partials)
        where TValue : struct
    {
        if (partials.Count == 0)
        {
            return 0;
        }

        var total = 0L;

        for (var i = 0; i < partials.Count; i++)
        {
            total += partials[i].Count;
        }

        return total;
    }

    /// <summary>
    /// Combines per-shard sums into a global sum.
    /// </summary>
    /// <typeparam name="TValue">The numeric type to sum.</typeparam>
    /// <param name="partials">The partial results from each shard.</param>
    /// <returns>The total sum across all shards, or zero if no partials.</returns>
    public static TValue CombineSum<TValue>(IReadOnlyList<ShardAggregatePartial<TValue>> partials)
        where TValue : struct, INumber<TValue>
    {
        if (partials.Count == 0)
        {
            return TValue.Zero;
        }

        var total = TValue.Zero;

        for (var i = 0; i < partials.Count; i++)
        {
            total += partials[i].Sum;
        }

        return total;
    }

    /// <summary>
    /// Combines per-shard sum and count into a correct global average using
    /// two-phase aggregation (totalSum / totalCount).
    /// </summary>
    /// <typeparam name="TValue">The numeric type to average.</typeparam>
    /// <param name="partials">The partial results from each shard.</param>
    /// <returns>
    /// The correct global average, or zero if the total count is zero.
    /// </returns>
    /// <remarks>
    /// This avoids the "average of averages" error. For example, if shard A has
    /// 1 item with value 10 and shard B has 99 items with value 1, the correct
    /// average is (10 + 99) / 100 = 1.09, not (10 + 1) / 2 = 5.5.
    /// </remarks>
    public static TValue CombineAvg<TValue>(IReadOnlyList<ShardAggregatePartial<TValue>> partials)
        where TValue : struct, INumber<TValue>
    {
        if (partials.Count == 0)
        {
            return TValue.Zero;
        }

        var totalSum = TValue.Zero;
        var totalCount = 0L;

        for (var i = 0; i < partials.Count; i++)
        {
            totalSum += partials[i].Sum;
            totalCount += partials[i].Count;
        }

        if (totalCount == 0)
        {
            return TValue.Zero;
        }

        return totalSum / TValue.CreateChecked(totalCount);
    }

    /// <summary>
    /// Combines per-shard minimums into a global minimum.
    /// </summary>
    /// <typeparam name="TValue">The type of the values to compare.</typeparam>
    /// <param name="partials">The partial results from each shard.</param>
    /// <returns>
    /// The global minimum value, or <c>null</c> if no partials have a minimum
    /// (i.e., all shards returned empty results).
    /// </returns>
    public static TValue? CombineMin<TValue>(IReadOnlyList<ShardAggregatePartial<TValue>> partials)
        where TValue : struct, IComparable<TValue>
    {
        if (partials.Count == 0)
        {
            return null;
        }

        TValue? globalMin = null;

        for (var i = 0; i < partials.Count; i++)
        {
            var shardMin = partials[i].Min;

            if (shardMin is null)
            {
                continue;
            }

            if (globalMin is null || shardMin.Value.CompareTo(globalMin.Value) < 0)
            {
                globalMin = shardMin;
            }
        }

        return globalMin;
    }

    /// <summary>
    /// Combines per-shard maximums into a global maximum.
    /// </summary>
    /// <typeparam name="TValue">The type of the values to compare.</typeparam>
    /// <param name="partials">The partial results from each shard.</param>
    /// <returns>
    /// The global maximum value, or <c>null</c> if no partials have a maximum
    /// (i.e., all shards returned empty results).
    /// </returns>
    public static TValue? CombineMax<TValue>(IReadOnlyList<ShardAggregatePartial<TValue>> partials)
        where TValue : struct, IComparable<TValue>
    {
        if (partials.Count == 0)
        {
            return null;
        }

        TValue? globalMax = null;

        for (var i = 0; i < partials.Count; i++)
        {
            var shardMax = partials[i].Max;

            if (shardMax is null)
            {
                continue;
            }

            if (globalMax is null || shardMax.Value.CompareTo(globalMax.Value) > 0)
            {
                globalMax = shardMax;
            }
        }

        return globalMax;
    }
}
