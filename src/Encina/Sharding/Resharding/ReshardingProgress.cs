namespace Encina.Sharding.Resharding;

/// <summary>
/// Provides real-time progress information for an active resharding operation.
/// </summary>
/// <param name="Id">The resharding operation identifier.</param>
/// <param name="CurrentPhase">The current workflow phase.</param>
/// <param name="OverallPercentComplete">Overall progress as a percentage (0.0 to 100.0).</param>
/// <param name="PerStepProgress">
/// Progress for each migration step, keyed by a step identifier
/// in the format <c>"sourceShardId→targetShardId"</c>.
/// </param>
public sealed record ReshardingProgress(
    Guid Id,
    ReshardingPhase CurrentPhase,
    double OverallPercentComplete,
    IReadOnlyDictionary<string, ShardMigrationProgress> PerStepProgress)
{
    /// <summary>
    /// Gets the per-step progress dictionary.
    /// </summary>
    public IReadOnlyDictionary<string, ShardMigrationProgress> PerStepProgress { get; } = PerStepProgress
        ?? throw new ArgumentNullException(nameof(PerStepProgress));
}
