using LanguageExt;

namespace Encina.Sharding.Resharding;

/// <summary>
/// Provides access to the external services required by the resharding workflow phases.
/// </summary>
/// <remarks>
/// <para>
/// This interface aggregates all external dependencies needed by the 6-phase resharding
/// workflow into a single injection point. The individual methods abstract over provider-specific
/// implementations (e.g., <c>IBulkOperations</c>, <c>IShardedCdcConnector</c>) that are
/// registered by satellite packages.
/// </para>
/// <para>
/// All methods follow the Railway-Oriented Programming pattern, returning
/// <c>Either&lt;EncinaError, T&gt;</c> for explicit error handling.
/// </para>
/// </remarks>
public interface IReshardingServices
{
    /// <summary>
    /// Copies a batch of rows from a source shard to a target shard for the specified key range.
    /// </summary>
    /// <param name="sourceShardId">The source shard identifier.</param>
    /// <param name="targetShardId">The target shard identifier.</param>
    /// <param name="keyRange">The hash ring range to copy.</param>
    /// <param name="batchSize">Maximum number of rows to copy in this batch.</param>
    /// <param name="lastPosition">The position after the last successfully copied batch, or <c>null</c> for the first batch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Right with the number of rows copied and the new batch position;
    /// Left with an <see cref="EncinaError"/> if the copy operation fails.
    /// </returns>
    Task<Either<EncinaError, CopyBatchResult>> CopyBatchAsync(
        string sourceShardId,
        string targetShardId,
        KeyRange keyRange,
        int batchSize,
        long? lastPosition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts streaming CDC change events from a specific shard, filtering by the given key ranges.
    /// </summary>
    /// <param name="sourceShardId">The shard to stream changes from.</param>
    /// <param name="targetShardId">The shard to apply changes to.</param>
    /// <param name="keyRange">The hash ring range to filter events for.</param>
    /// <param name="cdcPosition">The CDC position to start streaming from, or <c>null</c> to start from the latest position.</param>
    /// <param name="cancellationToken">Cancellation token to stop the stream.</param>
    /// <returns>
    /// Right with the replication result including rows replicated and final CDC position;
    /// Left with an <see cref="EncinaError"/> if the replication fails.
    /// </returns>
    Task<Either<EncinaError, ReplicationResult>> ReplicateChangesAsync(
        string sourceShardId,
        string targetShardId,
        KeyRange keyRange,
        string? cdcPosition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current CDC replication lag for a specific shard.
    /// </summary>
    /// <param name="sourceShardId">The source shard to check lag for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Right with the current lag duration;
    /// Left with an <see cref="EncinaError"/> if the lag cannot be determined.
    /// </returns>
    Task<Either<EncinaError, TimeSpan>> GetReplicationLagAsync(
        string sourceShardId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies data consistency between source and target shards for a key range.
    /// </summary>
    /// <param name="sourceShardId">The source shard identifier.</param>
    /// <param name="targetShardId">The target shard identifier.</param>
    /// <param name="keyRange">The hash ring range to verify.</param>
    /// <param name="mode">The verification strategy to use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Right with the verification result;
    /// Left with an <see cref="EncinaError"/> if verification cannot be performed.
    /// </returns>
    Task<Either<EncinaError, VerificationResult>> VerifyDataConsistencyAsync(
        string sourceShardId,
        string targetShardId,
        KeyRange keyRange,
        VerificationMode mode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically swaps the active shard topology to the new topology.
    /// </summary>
    /// <param name="newTopology">The new topology to activate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Right with <see cref="Unit"/> on success;
    /// Left with an <see cref="EncinaError"/> if the topology swap fails.
    /// </returns>
    Task<Either<EncinaError, Unit>> SwapTopologyAsync(
        ShardTopology newTopology,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes migrated rows from a source shard for the specified key range.
    /// </summary>
    /// <param name="sourceShardId">The source shard identifier.</param>
    /// <param name="keyRange">The hash ring range of rows to delete.</param>
    /// <param name="batchSize">Maximum number of rows to delete per batch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Right with the total number of rows deleted;
    /// Left with an <see cref="EncinaError"/> if the cleanup fails.
    /// </returns>
    Task<Either<EncinaError, long>> CleanupSourceDataAsync(
        string sourceShardId,
        KeyRange keyRange,
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the estimated row count for a key range on a specific shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <param name="keyRange">The hash ring range to estimate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Right with the estimated row count;
    /// Left with an <see cref="EncinaError"/> if the estimation fails.
    /// </returns>
    Task<Either<EncinaError, long>> EstimateRowCountAsync(
        string shardId,
        KeyRange keyRange,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a single batch copy operation during the copy phase.
/// </summary>
/// <param name="RowsCopied">The number of rows copied in this batch.</param>
/// <param name="NewBatchPosition">The position marker for the next batch, used for crash recovery.</param>
/// <param name="HasMoreRows">Whether there are more rows remaining to be copied.</param>
public sealed record CopyBatchResult(
    long RowsCopied,
    long NewBatchPosition,
    bool HasMoreRows);

/// <summary>
/// Result of a CDC replication pass for a migration step.
/// </summary>
/// <param name="RowsReplicated">The total number of rows replicated (inserts + updates + deletes).</param>
/// <param name="FinalCdcPosition">The final CDC position after replication, for persistence and recovery.</param>
/// <param name="CurrentLag">The current CDC lag at the end of the replication pass.</param>
public sealed record ReplicationResult(
    long RowsReplicated,
    string? FinalCdcPosition,
    TimeSpan CurrentLag);

/// <summary>
/// Result of a data verification check between source and target shards.
/// </summary>
/// <param name="IsConsistent"><c>true</c> if data is consistent between source and target.</param>
/// <param name="SourceRowCount">The row count on the source shard for the key range.</param>
/// <param name="TargetRowCount">The row count on the target shard for the key range.</param>
/// <param name="MismatchDetails">Optional description of mismatches found during verification.</param>
public sealed record VerificationResult(
    bool IsConsistent,
    long SourceRowCount,
    long TargetRowCount,
    string? MismatchDetails = null);
