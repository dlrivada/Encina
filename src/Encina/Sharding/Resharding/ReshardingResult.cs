namespace Encina.Sharding.Resharding;

/// <summary>
/// Represents the final outcome of a resharding operation.
/// </summary>
/// <param name="Id">The resharding operation identifier.</param>
/// <param name="FinalPhase">The terminal phase (Completed, RolledBack, or Failed).</param>
/// <param name="PhaseHistory">
/// Ordered history of completed phases with timing information.
/// </param>
/// <param name="RollbackMetadata">
/// Optional metadata for rollback support. Contains the original plan and topology
/// information needed to reverse a partial migration. <c>null</c> when the operation
/// completed successfully or was already rolled back.
/// </param>
public sealed record ReshardingResult(
    Guid Id,
    ReshardingPhase FinalPhase,
    IReadOnlyList<PhaseHistoryEntry> PhaseHistory,
    RollbackMetadata? RollbackMetadata)
{
    /// <summary>
    /// Gets the phase history list.
    /// </summary>
    public IReadOnlyList<PhaseHistoryEntry> PhaseHistory { get; } = PhaseHistory
        ?? throw new ArgumentNullException(nameof(PhaseHistory));

    /// <summary>
    /// Gets a value indicating whether the resharding completed successfully.
    /// </summary>
    public bool IsSuccess => FinalPhase == ReshardingPhase.Completed;
}
