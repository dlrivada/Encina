using LanguageExt;

namespace Encina.Sharding.Resharding;

/// <summary>
/// Coordinates the full 6-phase online resharding workflow: Plan → Copy → Replicate → Verify → Cutover → Cleanup.
/// </summary>
/// <remarks>
/// <para>
/// The resharding orchestrator automates data migration between shards with minimal downtime.
/// It integrates with <see cref="Routing.IShardRebalancer"/> for plan generation,
/// bulk operations for data copy, CDC for incremental replication, and topology providers
/// for atomic cutover.
/// </para>
/// <para>
/// All methods follow the Railway-Oriented Programming pattern, returning
/// <c>Either&lt;EncinaError, T&gt;</c> for explicit error handling without exceptions.
/// </para>
/// <para>
/// Resharding state is persisted via <see cref="IReshardingStateStore"/> to support
/// crash recovery. After a restart, active resharding operations can be resumed from
/// the last completed phase.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Plan a resharding operation
/// var request = new ReshardingRequest(oldTopology, newTopology);
/// var planResult = await orchestrator.PlanAsync(request, ct);
///
/// // Execute the plan with options
/// var result = await planResult.BindAsync(plan =>
///     orchestrator.ExecuteAsync(plan, options, ct));
///
/// // Handle result
/// result.Match(
///     Right: r => logger.LogInformation("Resharding completed: {Phase}", r.FinalPhase),
///     Left: error => logger.LogError("Resharding failed: {Error}", error.Message));
/// </code>
/// </example>
public interface IReshardingOrchestrator
{
    /// <summary>
    /// Generates a resharding plan by analyzing the differences between old and new topologies.
    /// </summary>
    /// <param name="request">The resharding request containing old and new topologies.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Right with the generated <see cref="ReshardingPlan"/> containing migration steps and estimates;
    /// Left with an <see cref="EncinaError"/> if plan generation fails (e.g., topologies are identical,
    /// shards are unreachable for row estimation).
    /// </returns>
    Task<Either<EncinaError, ReshardingPlan>> PlanAsync(
        ReshardingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the full 6-phase resharding workflow: Copy → Replicate → Verify → Cutover → Cleanup.
    /// </summary>
    /// <param name="plan">The resharding plan to execute.</param>
    /// <param name="options">Configuration options for the resharding operation.</param>
    /// <param name="cancellationToken">Cancellation token. Cancelling triggers graceful rollback.</param>
    /// <returns>
    /// Right with the <see cref="ReshardingResult"/> containing the final status and phase history;
    /// Left with an <see cref="EncinaError"/> if the operation fails and rollback is not possible.
    /// </returns>
    Task<Either<EncinaError, ReshardingResult>> ExecuteAsync(
        ReshardingPlan plan,
        ReshardingOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back a partial or failed resharding operation, restoring the original topology.
    /// </summary>
    /// <param name="result">The result of a failed or partially completed resharding operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Right with <see cref="LanguageExt.Unit"/> on successful rollback;
    /// Left with an <see cref="EncinaError"/> if rollback fails (e.g., data already cleaned up,
    /// no rollback metadata available).
    /// </returns>
    Task<Either<EncinaError, Unit>> RollbackAsync(
        ReshardingResult result,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the real-time progress of an active resharding operation.
    /// </summary>
    /// <param name="reshardingId">The unique identifier of the resharding operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Right with the <see cref="ReshardingProgress"/> containing current phase and per-step progress;
    /// Left with an <see cref="EncinaError"/> if the resharding ID is not found.
    /// </returns>
    Task<Either<EncinaError, ReshardingProgress>> GetProgressAsync(
        Guid reshardingId,
        CancellationToken cancellationToken = default);
}
