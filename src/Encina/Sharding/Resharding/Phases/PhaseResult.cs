namespace Encina.Sharding.Resharding.Phases;

/// <summary>
/// Represents the outcome of a single phase execution.
/// </summary>
/// <param name="Status">The final status of the phase.</param>
/// <param name="UpdatedProgress">The progress snapshot after the phase completed.</param>
/// <param name="UpdatedCheckpoint">Optional checkpoint data for crash recovery.</param>
public sealed record PhaseResult(
    PhaseStatus Status,
    ReshardingProgress UpdatedProgress,
    ReshardingCheckpoint? UpdatedCheckpoint = null)
{
    /// <summary>
    /// Gets the updated progress snapshot.
    /// </summary>
    public ReshardingProgress UpdatedProgress { get; } = UpdatedProgress
        ?? throw new ArgumentNullException(nameof(UpdatedProgress));
}

/// <summary>
/// Represents the outcome status of a phase execution.
/// </summary>
public enum PhaseStatus
{
    /// <summary>The phase completed successfully and the workflow should advance.</summary>
    Completed = 0,

    /// <summary>The phase was skipped (e.g., cleanup skipped by configuration).</summary>
    Skipped = 1,

    /// <summary>The phase was aborted by a user predicate (e.g., cutover aborted).</summary>
    Aborted = 2,
}
