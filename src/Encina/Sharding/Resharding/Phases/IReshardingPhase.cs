using LanguageExt;

namespace Encina.Sharding.Resharding.Phases;

/// <summary>
/// Represents a single executable phase within the resharding workflow.
/// </summary>
/// <remarks>
/// <para>
/// Each phase encapsulates one step of the 6-phase resharding process:
/// Planning, Copying, Replicating, Verifying, CuttingOver, and CleaningUp.
/// Phases are stateless and receive all required context via <see cref="PhaseContext"/>.
/// </para>
/// </remarks>
internal interface IReshardingPhase
{
    /// <summary>
    /// Gets the <see cref="ReshardingPhase"/> enum value that this implementation handles.
    /// </summary>
    ReshardingPhase Phase { get; }

    /// <summary>
    /// Executes the phase logic.
    /// </summary>
    /// <param name="context">The shared context for this execution.</param>
    /// <param name="cancellationToken">Cancellation token. Cancelling triggers graceful abort.</param>
    /// <returns>
    /// Right with a <see cref="PhaseResult"/> containing updated progress and checkpoint;
    /// Left with an <see cref="EncinaError"/> if the phase fails.
    /// </returns>
    Task<Either<EncinaError, PhaseResult>> ExecuteAsync(
        PhaseContext context,
        CancellationToken cancellationToken = default);
}
