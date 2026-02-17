using LanguageExt;

namespace Encina.Sharding.TimeBased;

/// <summary>
/// Routes queries and writes to shards based on timestamps, with tier-awareness
/// for hot/warm/cold/archived data lifecycle management.
/// </summary>
/// <remarks>
/// <para>
/// Extends <see cref="IShardRouter"/> with timestamp-based routing methods.
/// Each shard covers a contiguous time period (daily, weekly, monthly, quarterly, or yearly)
/// and belongs to a <see cref="ShardTier"/> that determines its mutability and storage characteristics.
/// </para>
/// <para>
/// Write operations are only permitted on <see cref="ShardTier.Hot"/> shards. Attempting to
/// route a write to a non-Hot shard returns error code <c>encina.sharding.shard_read_only</c>.
/// </para>
/// <para>
/// Range queries spanning multiple periods use <see cref="GetShardsInRangeAsync"/> to identify
/// all shards that overlap the requested time range, enabling scatter-gather across tiers.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Route a new event to the appropriate shard
/// var result = await router.RouteByTimestampAsync(DateTime.UtcNow);
/// result.Match(
///     Right: shardId => logger.LogInformation("Routed to {ShardId}", shardId),
///     Left: error => logger.LogError("Routing failed: {Error}", error.Message));
///
/// // Query all shards in a date range
/// var shards = await router.GetShardsInRangeAsync(
///     new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
///     new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
/// </code>
/// </example>
public interface ITimeBasedShardRouter : IShardRouter
{
    /// <summary>
    /// Routes a timestamp to the shard that covers the corresponding time period.
    /// </summary>
    /// <param name="timestamp">The UTC timestamp to route.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with the shard ID covering the timestamp's period;
    /// Left with an error if the timestamp falls outside all configured periods
    /// (code <c>encina.sharding.timestamp_outside_range</c>).
    /// </returns>
    Task<Either<EncinaError, string>> RouteByTimestampAsync(
        DateTime timestamp,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all shard IDs whose time periods overlap with the specified date range.
    /// </summary>
    /// <param name="from">The inclusive start of the time range (UTC).</param>
    /// <param name="toExclusive">The exclusive end of the time range (UTC).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with the ordered list of shard IDs covering the range (sorted by period start);
    /// Left with an error if no shards overlap the range.
    /// </returns>
    Task<Either<EncinaError, IReadOnlyList<string>>> GetShardsInRangeAsync(
        DateTime from,
        DateTime toExclusive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Routes a write operation to the shard covering the given timestamp, enforcing
    /// that the target shard is in the <see cref="ShardTier.Hot"/> tier.
    /// </summary>
    /// <param name="timestamp">The UTC timestamp to route.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with the shard ID if the shard is <see cref="ShardTier.Hot"/>;
    /// Left with error code <c>encina.sharding.shard_read_only</c> if the shard is not Hot;
    /// Left with error code <c>encina.sharding.timestamp_outside_range</c> if no shard covers the timestamp.
    /// </returns>
    Task<Either<EncinaError, string>> RouteWriteByTimestampAsync(
        DateTime timestamp,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current tier of a shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <returns>
    /// Right with the shard's current <see cref="ShardTier"/>;
    /// Left with an error if the shard is not found.
    /// </returns>
    Either<EncinaError, ShardTier> GetShardTier(string shardId);

    /// <summary>
    /// Gets the full tier metadata for a shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <returns>
    /// Right with the shard's <see cref="ShardTierInfo"/>;
    /// Left with an error if the shard is not found.
    /// </returns>
    Either<EncinaError, ShardTierInfo> GetShardTierInfo(string shardId);
}
