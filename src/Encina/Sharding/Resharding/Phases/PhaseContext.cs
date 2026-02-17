namespace Encina.Sharding.Resharding.Phases;

/// <summary>
/// Contains all the shared context needed by each phase during execution.
/// </summary>
/// <param name="ReshardingId">The unique identifier of the resharding operation.</param>
/// <param name="Plan">The resharding plan being executed.</param>
/// <param name="Options">The resharding configuration options.</param>
/// <param name="Progress">The current progress snapshot at the start of this phase.</param>
/// <param name="Checkpoint">The latest checkpoint for crash recovery, or <c>null</c> if starting fresh.</param>
/// <param name="Services">Shared services required by phase implementations.</param>
public sealed record PhaseContext(
    Guid ReshardingId,
    ReshardingPlan Plan,
    ReshardingOptions Options,
    ReshardingProgress Progress,
    ReshardingCheckpoint? Checkpoint,
    IReshardingServices Services)
{
    /// <summary>
    /// Gets the resharding plan.
    /// </summary>
    public ReshardingPlan Plan { get; } = Plan
        ?? throw new ArgumentNullException(nameof(Plan));

    /// <summary>
    /// Gets the resharding options.
    /// </summary>
    public ReshardingOptions Options { get; } = Options
        ?? throw new ArgumentNullException(nameof(Options));

    /// <summary>
    /// Gets the current progress.
    /// </summary>
    public ReshardingProgress Progress { get; } = Progress
        ?? throw new ArgumentNullException(nameof(Progress));

    /// <summary>
    /// Gets the shared services.
    /// </summary>
    public IReshardingServices Services { get; } = Services
        ?? throw new ArgumentNullException(nameof(Services));
}
