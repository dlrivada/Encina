namespace Encina.Sharding.Migrations;

/// <summary>
/// Tracks the migration outcome for a single shard, including timing and error details.
/// </summary>
/// <remarks>
/// <para>
/// Instances are created by the coordinator during migration execution and collected into
/// <see cref="MigrationResult.PerShardStatus"/> and <see cref="MigrationProgress.PerShardProgress"/>.
/// When <see cref="Outcome"/> is <see cref="MigrationOutcome.Failed"/>, the <see cref="Error"/>
/// property contains the <see cref="EncinaError"/> that caused the failure.
/// </para>
/// </remarks>
/// <param name="ShardId">The shard this status refers to.</param>
/// <param name="Outcome">The current outcome of the migration on this shard.</param>
/// <param name="Duration">Elapsed time for the migration on this shard.</param>
/// <param name="Error">
/// The error that occurred, or <see langword="null"/> when the migration succeeded or has not completed.
/// </param>
public sealed record ShardMigrationStatus(
    string ShardId,
    MigrationOutcome Outcome,
    TimeSpan Duration,
    EncinaError? Error = null)
{
    /// <summary>Gets the shard identifier.</summary>
    public string ShardId { get; } = !string.IsNullOrWhiteSpace(ShardId)
        ? ShardId
        : throw new ArgumentException("Shard ID cannot be null or whitespace.", nameof(ShardId));
}
