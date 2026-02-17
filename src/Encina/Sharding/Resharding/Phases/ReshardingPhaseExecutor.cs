using System.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Sharding.Resharding.Phases;

/// <summary>
/// Runs resharding phases sequentially, persists state after each successful phase,
/// and validates phase transitions.
/// </summary>
#pragma warning disable CA1848 // Use the LoggerMessage delegates
internal sealed class ReshardingPhaseExecutor
{
    private readonly IReshardingStateStore _stateStore;
    private readonly ILogger _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Ordered list of execution phases (excludes Planning which is handled separately).
    /// </summary>
    private static readonly ReshardingPhase[] ExecutionPhases =
    [
        ReshardingPhase.Copying,
        ReshardingPhase.Replicating,
        ReshardingPhase.Verifying,
        ReshardingPhase.CuttingOver,
        ReshardingPhase.CleaningUp,
    ];

    public ReshardingPhaseExecutor(
        IReshardingStateStore stateStore,
        ILogger logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(stateStore);
        ArgumentNullException.ThrowIfNull(logger);

        _stateStore = stateStore;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Executes the full resharding workflow from the first unfinished phase through completion.
    /// </summary>
    /// <param name="state">The current resharding state (possibly resumed after crash).</param>
    /// <param name="phases">The phase implementations keyed by <see cref="ReshardingPhase"/>.</param>
    /// <param name="context">The shared execution context.</param>
    /// <param name="options">The resharding options for callbacks.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Right with a list of <see cref="PhaseHistoryEntry"/> for all completed phases;
    /// Left with an <see cref="EncinaError"/> if any phase fails.
    /// </returns>
    public async Task<Either<EncinaError, IReadOnlyList<PhaseHistoryEntry>>> ExecuteAllPhasesAsync(
        ReshardingState state,
        IReadOnlyDictionary<ReshardingPhase, IReshardingPhase> phases,
        PhaseContext context,
        ReshardingOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(phases);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

        var history = new List<PhaseHistoryEntry>();
        var currentProgress = context.Progress;
        var currentCheckpoint = context.Checkpoint;

        // Determine where to start (for crash recovery, skip already-completed phases)
        var startIndex = GetStartIndex(state.LastCompletedPhase);

        for (var i = startIndex; i < ExecutionPhases.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var phase = ExecutionPhases[i];

            if (!phases.TryGetValue(phase, out var phaseImpl))
            {
                return Either<EncinaError, IReadOnlyList<PhaseHistoryEntry>>.Left(
                    EncinaErrors.Create(
                        ReshardingErrorCodes.InvalidPhaseTransition,
                        $"No implementation registered for phase '{phase}'."));
            }

            // Validate transition
            var transitionResult = ValidateTransition(state.LastCompletedPhase, phase);
            if (transitionResult.IsLeft)
            {
                return transitionResult.Map<IReadOnlyList<PhaseHistoryEntry>>(_ => history);
            }

            // Execute the phase
            var phaseContext = new PhaseContext(
                context.ReshardingId,
                context.Plan,
                context.Options,
                currentProgress,
                currentCheckpoint,
                context.Services);

            _logger.LogInformation(
                "Phase starting. ReshardingId={ReshardingId}, Phase={Phase}",
                state.Id, phase);

            var phaseStart = _timeProvider.GetUtcNow().UtcDateTime;
            var sw = Stopwatch.GetTimestamp();

            var phaseResult = await phaseImpl.ExecuteAsync(phaseContext, cancellationToken);

            if (phaseResult.IsLeft)
            {
                var error = phaseResult.Match(Right: _ => default!, Left: e => e);
                _logger.LogError(
                    "Phase failed. ReshardingId={ReshardingId}, Phase={Phase}, ErrorCode={ErrorCode}",
                    state.Id, phase, error.GetCode().IfNone("unknown"));
                return Either<EncinaError, IReadOnlyList<PhaseHistoryEntry>>.Left(error);
            }

            var result = phaseResult.Match(Right: r => r, Left: _ => default!);
            var phaseEnd = _timeProvider.GetUtcNow().UtcDateTime;
            var elapsed = Stopwatch.GetElapsedTime(sw);

            _logger.LogInformation(
                "Phase completed. ReshardingId={ReshardingId}, Phase={Phase}, Status={Status}, Duration={DurationMs:F1}ms",
                state.Id, phase, result.Status, elapsed.TotalMilliseconds);

            // Handle aborted phases (e.g., cutover aborted by predicate)
            if (result.Status == PhaseStatus.Aborted)
            {
                history.Add(new PhaseHistoryEntry(phase, phaseStart, phaseEnd));
                return Either<EncinaError, IReadOnlyList<PhaseHistoryEntry>>.Left(
                    EncinaErrors.Create(
                        ReshardingErrorCodes.CutoverAborted,
                        $"Phase '{phase}' was aborted."));
            }

            // Update state
            currentProgress = result.UpdatedProgress;
            currentCheckpoint = result.UpdatedCheckpoint;
            history.Add(new PhaseHistoryEntry(phase, phaseStart, phaseEnd));

            // Persist state after each successful phase
            var newState = new ReshardingState(
                state.Id,
                GetNextPhaseOrCompleted(phase),
                state.Plan,
                currentProgress,
                phase,
                state.StartedAtUtc,
                currentCheckpoint);

            var saveResult = await _stateStore.SaveStateAsync(newState, cancellationToken);
            if (saveResult.IsLeft)
            {
                return saveResult.Map<IReadOnlyList<PhaseHistoryEntry>>(_ => history);
            }

            // Update state for next iteration
            state = newState;

            // Invoke phase-completed callback
            if (options.OnPhaseCompleted is not null)
            {
                try
                {
                    await options.OnPhaseCompleted(phase, currentProgress);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "OnPhaseCompleted callback failed. ReshardingId={ReshardingId}, Phase={Phase}",
                        state.Id, phase);
                    // Callback failures are non-fatal — the phase already succeeded
                }
            }
        }

        return Either<EncinaError, IReadOnlyList<PhaseHistoryEntry>>.Right(history);
    }

    /// <summary>
    /// Validates that the transition from a completed phase to the next phase is valid.
    /// </summary>
    internal static Either<EncinaError, Unit> ValidateTransition(
        ReshardingPhase? lastCompleted,
        ReshardingPhase next)
    {
        var expectedPrevious = next switch
        {
            ReshardingPhase.Copying => (ReshardingPhase?)null,
            ReshardingPhase.Replicating => ReshardingPhase.Copying,
            ReshardingPhase.Verifying => ReshardingPhase.Replicating,
            ReshardingPhase.CuttingOver => ReshardingPhase.Verifying,
            ReshardingPhase.CleaningUp => ReshardingPhase.CuttingOver,
            _ => (ReshardingPhase?)(-1), // Invalid target phase
        };

        if ((int?)expectedPrevious == -1)
        {
            return Either<EncinaError, Unit>.Left(
                EncinaErrors.Create(
                    ReshardingErrorCodes.InvalidPhaseTransition,
                    $"Cannot transition to '{next}': not a valid execution phase."));
        }

        if (lastCompleted != expectedPrevious)
        {
            return Either<EncinaError, Unit>.Left(
                EncinaErrors.Create(
                    ReshardingErrorCodes.InvalidPhaseTransition,
                    $"Cannot transition to '{next}': expected previous phase '{expectedPrevious}' but was '{lastCompleted}'."));
        }

        return Either<EncinaError, Unit>.Right(unit);
    }

    private static int GetStartIndex(ReshardingPhase? lastCompleted)
    {
        if (lastCompleted is null)
        {
            return 0; // Start from Copying
        }

        // Find the index after the last completed phase
        for (var i = 0; i < ExecutionPhases.Length; i++)
        {
            if (ExecutionPhases[i] == lastCompleted.Value)
            {
                return i + 1;
            }
        }

        return 0;
    }

    private static ReshardingPhase GetNextPhaseOrCompleted(ReshardingPhase current)
    {
        for (var i = 0; i < ExecutionPhases.Length; i++)
        {
            if (ExecutionPhases[i] == current && i + 1 < ExecutionPhases.Length)
            {
                return ExecutionPhases[i + 1];
            }
        }

        return ReshardingPhase.Completed;
    }
}
#pragma warning restore CA1848
