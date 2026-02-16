namespace Encina.Sharding.Resharding;

/// <summary>
/// Describes a complete resharding plan with all migration steps and resource estimates.
/// </summary>
/// <param name="Id">Unique identifier for this resharding plan.</param>
/// <param name="Steps">The ordered list of migration steps to execute.</param>
/// <param name="Estimate">Estimated resource requirements for the full operation.</param>
public sealed record ReshardingPlan(
    Guid Id,
    IReadOnlyList<ShardMigrationStep> Steps,
    EstimatedResources Estimate)
{
    /// <summary>
    /// Gets the ordered list of migration steps.
    /// </summary>
    public IReadOnlyList<ShardMigrationStep> Steps { get; } = Steps
        ?? throw new ArgumentNullException(nameof(Steps));

    /// <summary>
    /// Gets the estimated resource requirements.
    /// </summary>
    public EstimatedResources Estimate { get; } = Estimate
        ?? throw new ArgumentNullException(nameof(Estimate));
}
