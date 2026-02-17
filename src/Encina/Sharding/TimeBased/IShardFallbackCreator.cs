using LanguageExt;

namespace Encina.Sharding.TimeBased;

/// <summary>
/// Creates a missing shard on-demand when the <see cref="ITimeBasedShardRouter"/>
/// cannot find a shard covering a requested timestamp.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides a fallback mechanism for resilience when the
/// <see cref="TierTransitionScheduler"/> misses its auto-creation window.
/// When a routing request targets a time period with no configured shard,
/// the router delegates to this interface to create the shard just-in-time.
/// </para>
/// <para>
/// Implementations should be idempotent. Concurrent calls for the same period
/// may occur in multi-instance deployments; the implementation must handle
/// duplicate creation attempts gracefully (e.g., via a distributed lock or
/// optimistic concurrency).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class DefaultShardFallbackCreator : IShardFallbackCreator
/// {
///     public async Task&lt;Either&lt;EncinaError, ShardTierInfo&gt;&gt; CreateShardForTimestampAsync(
///         DateTime timestamp, CancellationToken ct)
///     {
///         // Compute period, create database, register in tier store
///     }
/// }
/// </code>
/// </example>
public interface IShardFallbackCreator
{
    /// <summary>
    /// Creates a new shard covering the time period that contains the given timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp that could not be routed to an existing shard.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with the newly created <see cref="ShardTierInfo"/> on success;
    /// Left with an error if creation fails.
    /// </returns>
    Task<Either<EncinaError, ShardTierInfo>> CreateShardForTimestampAsync(
        DateTime timestamp,
        CancellationToken cancellationToken = default);
}
