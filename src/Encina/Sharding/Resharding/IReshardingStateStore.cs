using LanguageExt;

namespace Encina.Sharding.Resharding;

/// <summary>
/// Persists resharding operation state for crash recovery and progress tracking.
/// </summary>
/// <remarks>
/// <para>
/// The state store enables the resharding orchestrator to resume interrupted operations
/// from the last completed phase after a process restart. All state mutations are atomic
/// to prevent partial state corruption.
/// </para>
/// <para>
/// Implementations should be backed by a durable store (e.g., database table) following
/// the same pattern as <c>ICdcPositionStore</c> in the CDC module.
/// </para>
/// </remarks>
public interface IReshardingStateStore
{
    /// <summary>
    /// Persists the current state of a resharding operation.
    /// </summary>
    /// <param name="state">The state to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Right with <see cref="Unit"/> on success;
    /// Left with an <see cref="EncinaError"/> if the save fails.
    /// </returns>
    Task<Either<EncinaError, Unit>> SaveStateAsync(
        ReshardingState state,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the state of a specific resharding operation.
    /// </summary>
    /// <param name="reshardingId">The unique identifier of the resharding operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Right with the <see cref="ReshardingState"/> if found;
    /// Left with an <see cref="EncinaError"/> if the state is not found or retrieval fails.
    /// </returns>
    Task<Either<EncinaError, ReshardingState>> GetStateAsync(
        Guid reshardingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active (non-terminal) resharding operations for crash recovery.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Right with a list of active <see cref="ReshardingState"/> entries
    /// (those not in Completed, RolledBack, or Failed phase);
    /// Left with an <see cref="EncinaError"/> if retrieval fails.
    /// </returns>
    Task<Either<EncinaError, IReadOnlyList<ReshardingState>>> GetActiveReshardingsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the state of a completed or rolled-back resharding operation.
    /// </summary>
    /// <param name="reshardingId">The unique identifier of the resharding operation to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Right with <see cref="Unit"/> on success;
    /// Left with an <see cref="EncinaError"/> if the deletion fails.
    /// </returns>
    Task<Either<EncinaError, Unit>> DeleteStateAsync(
        Guid reshardingId,
        CancellationToken cancellationToken = default);
}
