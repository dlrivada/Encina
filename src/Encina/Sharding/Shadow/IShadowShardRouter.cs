using LanguageExt;

namespace Encina.Sharding.Shadow;

/// <summary>
/// Extends <see cref="IShardRouter"/> with shadow-specific routing capabilities for
/// production testing of new shard topologies.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IShadowShardRouter"/> enables comparing routing decisions between a production
/// topology and a shadow topology under real traffic. All <see cref="IShardRouter"/> methods
/// delegate to the production router, keeping the production path unchanged.
/// </para>
/// <para>
/// Shadow-specific operations (<see cref="RouteShadowAsync(string, CancellationToken)"/>
/// and <see cref="CompareAsync"/>) route against the shadow topology and measure latency
/// for comparison. Shadow failures never affect the production path.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Check if shadow routing is active
/// if (shadowRouter.IsShadowEnabled)
/// {
///     // Compare routing decisions between production and shadow topologies
///     var comparison = await shadowRouter.CompareAsync("customer-123", cancellationToken);
///
///     if (!comparison.RoutingMatch)
///     {
///         logger.LogWarning(
///             "Routing mismatch: production={ProdShard}, shadow={ShadowShard}",
///             comparison.ProductionShardId, comparison.ShadowShardId);
///     }
///
///     // Route using the shadow topology only (for diagnostics)
///     var shadowResult = await shadowRouter.RouteShadowAsync("customer-123", cancellationToken);
///     shadowResult.Match(
///         Right: shardId => logger.LogDebug("Shadow routed to {ShardId}", shardId),
///         Left: error => logger.LogWarning("Shadow routing failed: {Error}", error.Message));
/// }
/// </code>
/// </example>
public interface IShadowShardRouter : IShardRouter
{
    /// <summary>
    /// Gets a value indicating whether shadow routing is currently enabled.
    /// </summary>
    /// <value>
    /// <c>true</c> if the shadow topology is configured and shadow operations are active;
    /// <c>false</c> otherwise.
    /// </value>
    bool IsShadowEnabled { get; }

    /// <summary>
    /// Gets the shadow shard topology used for shadow routing operations.
    /// </summary>
    /// <value>The <see cref="ShardTopology"/> representing the shadow (new) topology.</value>
    /// <remarks>
    /// This property exposes the shadow topology for diagnostics, monitoring dashboards,
    /// and operational tooling. The production topology is accessible via the standard
    /// <see cref="IShardRouter"/> methods.
    /// </remarks>
    ShardTopology ShadowTopology { get; }

    /// <summary>
    /// Routes a shard key using the shadow topology only.
    /// </summary>
    /// <param name="shardKey">The shard key to route against the shadow topology.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// Right with the shadow shard ID if routing succeeds;
    /// Left with an <see cref="EncinaError"/> if the shadow routing fails.
    /// </returns>
    /// <remarks>
    /// This method only routes against the shadow topology and does not affect the production
    /// path. It is useful for diagnostics and for pre-validating the shadow topology before
    /// enabling dual-write or shadow reads.
    /// </remarks>
    Task<Either<EncinaError, string>> RouteShadowAsync(string shardKey, CancellationToken ct);

    /// <summary>
    /// Routes a compound shard key using the shadow topology only.
    /// </summary>
    /// <param name="key">The compound shard key to route against the shadow topology.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// Right with the shadow shard ID if routing succeeds;
    /// Left with an <see cref="EncinaError"/> if the shadow routing fails.
    /// </returns>
    /// <remarks>
    /// This overload supports compound keys with multiple components. The shadow router
    /// applies the same key serialization rules as the production router.
    /// </remarks>
    Task<Either<EncinaError, string>> RouteShadowAsync(CompoundShardKey key, CancellationToken ct);

    /// <summary>
    /// Compares routing decisions between the production and shadow topologies for a given shard key.
    /// </summary>
    /// <param name="shardKey">The shard key to route against both topologies.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ShadowComparisonResult"/> containing the production and shadow routing results,
    /// whether they match, and latency measurements for each operation.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Both the production and shadow routers are invoked, and their latencies are measured
    /// independently using <see cref="System.Diagnostics.Stopwatch.GetTimestamp()"/>. If
    /// the shadow router fails, the comparison result will contain the error information
    /// without affecting the production result.
    /// </para>
    /// </remarks>
    Task<ShadowComparisonResult> CompareAsync(string shardKey, CancellationToken ct);
}
