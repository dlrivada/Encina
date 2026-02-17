namespace Encina.Sharding.Resharding;

/// <summary>
/// Contains the information needed to rollback a partial resharding operation.
/// </summary>
/// <param name="OriginalPlan">The resharding plan that was being executed.</param>
/// <param name="OldTopology">The original topology before the resharding started.</param>
/// <param name="LastCompletedPhase">The last phase that completed successfully before failure.</param>
public sealed record RollbackMetadata(
    ReshardingPlan OriginalPlan,
    ShardTopology OldTopology,
    ReshardingPhase LastCompletedPhase)
{
    /// <summary>
    /// Gets the original resharding plan.
    /// </summary>
    public ReshardingPlan OriginalPlan { get; } = OriginalPlan
        ?? throw new ArgumentNullException(nameof(OriginalPlan));

    /// <summary>
    /// Gets the original topology.
    /// </summary>
    public ShardTopology OldTopology { get; } = OldTopology
        ?? throw new ArgumentNullException(nameof(OldTopology));
}
