using LanguageExt;

namespace Encina.Sharding.TimeBased;

/// <summary>
/// Manages tier transitions, read-only enforcement, archival, and retention for time-based shards.
/// </summary>
/// <remarks>
/// <para>
/// The archiver coordinates tier lifecycle operations. A tier transition typically involves:
/// updating the tier in the <see cref="ITierStore"/>, optionally enforcing read-only at
/// the database level, and optionally exporting data to archival storage.
/// </para>
/// <para>
/// All methods return <c>Either&lt;EncinaError, Unit&gt;</c> following the Railway Oriented
/// Programming pattern. Failures are reported as <c>Left</c> values with specific error codes
/// (e.g., <c>encina.sharding.tier_transition_failed</c>, <c>encina.sharding.archival_failed</c>).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Transition a shard from Hot to Warm
/// var result = await archiver.TransitionTierAsync("orders-2025-10", ShardTier.Warm);
/// result.Match(
///     Right: _ => logger.LogInformation("Transition complete"),
///     Left: error => logger.LogError("Transition failed: {Error}", error.Message));
///
/// // Archive a cold shard to external storage
/// var archiveResult = await archiver.ArchiveShardAsync("orders-2024-01",
///     new ArchiveOptions("s3://archive-bucket/orders/2024-01"));
/// </code>
/// </example>
public interface IShardArchiver
{
    /// <summary>
    /// Transitions a shard to a new tier, updating the tier store and optionally
    /// enforcing read-only at the database level.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="newTier">The target tier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with <see cref="Unit"/> on success;
    /// Left with error code <c>encina.sharding.tier_transition_failed</c> on failure.
    /// </returns>
    Task<Either<EncinaError, Unit>> TransitionTierAsync(
        string shardId,
        ShardTier newTier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives shard data to an external storage destination.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="options">The archive configuration (destination, compression, etc.).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with <see cref="Unit"/> on success;
    /// Left with error code <c>encina.sharding.archival_failed</c> on failure.
    /// </returns>
    Task<Either<EncinaError, Unit>> ArchiveShardAsync(
        string shardId,
        ArchiveOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enforces read-only state on a shard at the application or database level.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with <see cref="Unit"/> on success;
    /// Left with an error if enforcement fails.
    /// </returns>
    Task<Either<EncinaError, Unit>> EnforceReadOnlyAsync(
        string shardId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes shard data as part of a retention policy.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with <see cref="Unit"/> on success;
    /// Left with error code <c>encina.sharding.retention_policy_failed</c> on failure.
    /// </returns>
    /// <remarks>
    /// The actual data deletion is provider-specific. The default implementation
    /// updates the tier store only. Database-level deletion requires a provider-specific
    /// <see cref="IShardArchiver"/> implementation.
    /// </remarks>
    Task<Either<EncinaError, Unit>> DeleteShardDataAsync(
        string shardId,
        CancellationToken cancellationToken = default);
}
