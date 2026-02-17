namespace Encina.Sharding.Resharding;

/// <summary>
/// Records the completion of a resharding phase with timing information.
/// </summary>
/// <param name="Phase">The phase that was completed.</param>
/// <param name="StartedAtUtc">When the phase started.</param>
/// <param name="CompletedAtUtc">When the phase completed.</param>
public sealed record PhaseHistoryEntry(
    ReshardingPhase Phase,
    DateTime StartedAtUtc,
    DateTime CompletedAtUtc)
{
    /// <summary>
    /// Gets the duration of this phase.
    /// </summary>
    public TimeSpan Duration => CompletedAtUtc - StartedAtUtc;
}
