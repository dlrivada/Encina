namespace Encina.Sharding;

/// <summary>
/// Intermediate result from a single shard during a two-phase distributed aggregation.
/// </summary>
/// <typeparam name="TValue">The numeric type of the aggregated values.</typeparam>
/// <param name="ShardId">The shard that produced this partial result.</param>
/// <param name="Sum">The sum of the selected field on this shard (used for Sum and Avg).</param>
/// <param name="Count">The count of matching entities on this shard (used for Count and Avg).</param>
/// <param name="Min">The minimum value of the selected field on this shard (null if no entities matched).</param>
/// <param name="Max">The maximum value of the selected field on this shard (null if no entities matched).</param>
/// <remarks>
/// <para>
/// This record captures all intermediate values needed for two-phase aggregation.
/// A single partial can support multiple aggregation types:
/// </para>
/// <list type="bullet">
///   <item><description><b>Count</b>: Uses <see cref="Count"/> only.</description></item>
///   <item><description><b>Sum</b>: Uses <see cref="Sum"/> only.</description></item>
///   <item><description><b>Avg</b>: Uses both <see cref="Sum"/> and <see cref="Count"/> to compute totalSum / totalCount.</description></item>
///   <item><description><b>Min</b>: Uses <see cref="Min"/> only.</description></item>
///   <item><description><b>Max</b>: Uses <see cref="Max"/> only.</description></item>
/// </list>
/// </remarks>
public sealed record ShardAggregatePartial<TValue>(
    string ShardId,
    TValue Sum,
    long Count,
    TValue? Min,
    TValue? Max)
    where TValue : struct;
