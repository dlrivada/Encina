namespace Encina.Sharding.Shadow;

/// <summary>
/// Captures the result of comparing routing decisions between a production and shadow shard topology.
/// </summary>
/// <remarks>
/// <para>
/// This immutable record is produced by <see cref="IShadowShardRouter.CompareAsync"/> and contains
/// the shard routing result from both topologies, whether the routing agrees, and latency measurements
/// for each routing operation.
/// </para>
/// <para>
/// For shadow reads (when <see cref="ShadowShardingOptions.ShadowReadPercentage"/> is greater than zero),
/// <see cref="ResultsMatch"/> indicates whether the query results from both topologies agree. For shadow
/// writes (dual-write), <see cref="ResultsMatch"/> is <c>null</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await shadowRouter.CompareAsync("customer-123", cancellationToken);
///
/// if (!result.RoutingMatch)
/// {
///     logger.LogWarning(
///         "Routing mismatch for {ShardKey}: production={ProdShard}, shadow={ShadowShard}",
///         result.ShardKey, result.ProductionShardId, result.ShadowShardId);
/// }
///
/// logger.LogInformation(
///     "Latency difference: {Diff:F2}ms",
///     result.LatencyDifference.TotalMilliseconds);
/// </code>
/// </example>
/// <param name="ShardKey">The shard key that was routed.</param>
/// <param name="ProductionShardId">The shard ID returned by the production topology.</param>
/// <param name="ShadowShardId">The shard ID returned by the shadow topology.</param>
/// <param name="RoutingMatch">
/// <c>true</c> if both the production and shadow topologies routed to the same shard ID;
/// <c>false</c> if the routing decisions differ.
/// </param>
/// <param name="ProductionLatency">The time taken to route using the production topology.</param>
/// <param name="ShadowLatency">The time taken to route using the shadow topology.</param>
/// <param name="ResultsMatch">
/// <c>true</c> if the query results from both topologies match; <c>false</c> if they differ;
/// <c>null</c> if result comparison was not performed (e.g., for writes or when
/// <see cref="ShadowShardingOptions.CompareResults"/> is disabled).
/// </param>
/// <param name="ComparedAt">The UTC timestamp when the comparison was performed.</param>
public record ShadowComparisonResult(
    string ShardKey,
    string ProductionShardId,
    string ShadowShardId,
    bool RoutingMatch,
    TimeSpan ProductionLatency,
    TimeSpan ShadowLatency,
    bool? ResultsMatch,
    DateTimeOffset ComparedAt)
{
    /// <summary>
    /// Gets the latency difference between the shadow and production routing operations.
    /// </summary>
    /// <value>
    /// A positive value indicates the shadow topology was slower; a negative value indicates
    /// the shadow topology was faster.
    /// </value>
    public TimeSpan LatencyDifference => ShadowLatency - ProductionLatency;
}
