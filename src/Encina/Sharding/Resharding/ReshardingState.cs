namespace Encina.Sharding.Resharding;

/// <summary>
/// Persistent state of an active resharding operation for crash recovery.
/// </summary>
/// <param name="Id">Unique identifier for the resharding operation.</param>
/// <param name="CurrentPhase">The current phase of the operation.</param>
/// <param name="Plan">The resharding plan being executed.</param>
/// <param name="Progress">The current progress of the operation.</param>
/// <param name="LastCompletedPhase">The last phase that completed successfully, or <c>null</c> if none have completed.</param>
/// <param name="StartedAtUtc">When the resharding operation started.</param>
/// <param name="Checkpoint">
/// Phase-specific checkpoint data for resuming after a crash.
/// Contains batch positions for the copy phase and CDC positions for the replication phase.
/// </param>
public sealed record ReshardingState(
    Guid Id,
    ReshardingPhase CurrentPhase,
    ReshardingPlan Plan,
    ReshardingProgress Progress,
    ReshardingPhase? LastCompletedPhase,
    DateTime StartedAtUtc,
    ReshardingCheckpoint? Checkpoint)
{
    /// <summary>
    /// Gets the resharding plan.
    /// </summary>
    public ReshardingPlan Plan { get; } = Plan
        ?? throw new ArgumentNullException(nameof(Plan));

    /// <summary>
    /// Gets the current progress.
    /// </summary>
    public ReshardingProgress Progress { get; } = Progress
        ?? throw new ArgumentNullException(nameof(Progress));
}
