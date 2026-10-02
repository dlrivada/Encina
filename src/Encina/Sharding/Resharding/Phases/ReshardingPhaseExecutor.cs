using System.Diagnostics;
using Encina.Diagnostics;
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

        var run = new PhaseRun(state, context);

        // Determine where to start (for crash recovery, skip already-completed phases)
        var startIndex = GetStartIndex(state.LastCompletedPhase);

        for (var i = startIndex; i < ExecutionPhases.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var failure = await ExecutePhaseAsync(run, ExecutionPhases[i], phases, context, options, cancellationToken);
            if (failure is { } error)
            {
                return error;
            }
        }

        return Either<EncinaError, IReadOnlyList<PhaseHistoryEntry>>.Right(run.History);
    }

    /// <summary>
    /// Runs one phase: resolves and validates it, executes it, records it and persists the state.
    /// Returns <c>null</c> when the phase succeeded, otherwise the failure that ends the workflow.
    /// </summary>
    private async Task<Either<EncinaError, IReadOnlyList<PhaseHistoryEntry>>?> ExecutePhaseAsync(
        PhaseRun run,
        ReshardingPhase phase,
        IReadOnlyDictionary<ReshardingPhase, IReshardingPhase> phases,
        PhaseContext context,
        ReshardingOptions options,
        CancellationToken cancellationToken)
    {
        var resolution = ResolvePhase(run.State.LastCompletedPhase, phase, phases, out var phaseImpl);
        if (resolution.IsLeft)
        {
            return Failure(resolution);
        }

        // Execute the phase
        var phaseContext = new PhaseContext(
            context.ReshardingId,
            context.Plan,
            context.Options,
            run.Progress,
            run.Checkpoint,
            context.Services);

        _logger.LogInformation(
            "Phase starting. ReshardingId={ReshardingId}, Phase={Phase}",
            run.State.Id, phase);

        var phaseStart = _timeProvider.GetUtcNow().UtcDateTime;
        var sw = Stopwatch.GetTimestamp();

        var phaseResult = await phaseImpl.ExecuteAsync(phaseContext, cancellationToken);

        if (phaseResult.IsLeft)
        {
            var error = LeftOf(phaseResult);
            _logger.LogError(
                "Phase failed. ReshardingId={ReshardingId}, Phase={Phase}, ErrorCode={ErrorCode}",
                run.State.Id, phase, error.GetCode().IfNone("unknown"));
            return Either<EncinaError, IReadOnlyList<PhaseHistoryEntry>>.Left(error);
        }

        var result = RightOf(phaseResult);
        var phaseEnd = _timeProvider.GetUtcNow().UtcDateTime;

        _logger.LogInformation(
            "Phase completed. ReshardingId={ReshardingId}, Phase={Phase}, Status={Status}, Duration={DurationMs:F1}ms",
            run.State.Id, phase, result.Status, Stopwatch.GetElapsedTime(sw).TotalMilliseconds);

        run.History.Add(new PhaseHistoryEntry(phase, phaseStart, phaseEnd));

        // Handle aborted phases (e.g., cutover aborted by predicate)
        if (result.Status == PhaseStatus.Aborted)
        {
            return Either<EncinaError, IReadOnlyList<PhaseHistoryEntry>>.Left(
                EncinaErrors.Create(
                    ReshardingErrorCodes.CutoverAborted,
                    $"Phase '{phase}' was aborted."));
        }

        run.Progress = result.UpdatedProgress;
        run.Checkpoint = result.UpdatedCheckpoint;

        var saveResult = await PersistStateAsync(run, phase, cancellationToken);
        if (saveResult.IsLeft)
        {
            return Failure(saveResult);
        }

        await InvokePhaseCompletedCallbackAsync(options, run, phase);
        return null;
    }

    private static Either<EncinaError, Unit> ResolvePhase(
        ReshardingPhase? lastCompleted,
        ReshardingPhase phase,
        IReadOnlyDictionary<ReshardingPhase, IReshardingPhase> phases,
        out IReshardingPhase phaseImpl)
    {
        if (!phases.TryGetValue(phase, out phaseImpl!))
        {
            return Either<EncinaError, Unit>.Left(
                EncinaErrors.Create(
                    ReshardingErrorCodes.InvalidPhaseTransition,
                    $"No implementation registered for phase '{phase}'."));
        }

        // Validate transition
        return ValidateTransition(lastCompleted, phase);
    }

    private static EncinaError LeftOf<T>(Either<EncinaError, T> either) =>
        either.Match(Right: _ => default!, Left: e => e);

    private static T RightOf<T>(Either<EncinaError, T> either) =>
        either.Match(Right: r => r, Left: _ => default!);

    /// <summary>Persists the state after a successful phase and moves the run on to it.</summary>
    private async Task<Either<EncinaError, Unit>> PersistStateAsync(
        PhaseRun run,
        ReshardingPhase phase,
        CancellationToken cancellationToken)
    {
        var newState = new ReshardingState(
            run.State.Id,
            GetNextPhaseOrCompleted(phase),
            run.State.Plan,
            run.Progress,
            phase,
            run.State.StartedAtUtc,
            run.Checkpoint);

        var saveResult = await _stateStore.SaveStateAsync(newState, cancellationToken);
        if (saveResult.IsRight)
        {
            // Update state for next iteration
            run.State = newState;
        }

        return saveResult;
    }

    private async Task InvokePhaseCompletedCallbackAsync(ReshardingOptions options, PhaseRun run, ReshardingPhase phase)
    {
        if (options.OnPhaseCompleted is null)
        {
            return;
        }

        try
        {
            await options.OnPhaseCompleted(phase, run.Progress);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex.ForLogging(),
                "OnPhaseCompleted callback failed. ReshardingId={ReshardingId}, Phase={Phase}",
                run.State.Id, phase);
            // Callback failures are non-fatal — the phase already succeeded
        }
    }

    private static Either<EncinaError, IReadOnlyList<PhaseHistoryEntry>> Failure<T>(Either<EncinaError, T> failed) =>
        failed.Match(
            Right: _ => default!,
            Left: e => Either<EncinaError, IReadOnlyList<PhaseHistoryEntry>>.Left(e));

    /// <summary>The mutable state of one workflow run: the persisted state, progress, checkpoint and history so far.</summary>
    private sealed class PhaseRun(ReshardingState state, PhaseContext context)
    {
        public ReshardingState State { get; set; } = state;

        public ReshardingProgress Progress { get; set; } = context.Progress;

        public ReshardingCheckpoint? Checkpoint { get; set; } = context.Checkpoint;

        public List<PhaseHistoryEntry> History { get; } = [];
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
