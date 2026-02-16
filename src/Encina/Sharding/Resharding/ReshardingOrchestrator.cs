using System.Collections.Concurrent;
using Encina.Sharding.Resharding.Phases;
using Encina.Sharding.Routing;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Sharding.Resharding;

/// <summary>
/// Coordinates the full 6-phase online resharding workflow with crash recovery support.
/// </summary>
#pragma warning disable CA1848 // Use the LoggerMessage delegates
internal sealed class ReshardingOrchestrator : IReshardingOrchestrator
{
    private readonly IShardRebalancer _rebalancer;
    private readonly IShardTopologyProvider _topologyProvider;
    private readonly IReshardingStateStore _stateStore;
    private readonly IReshardingServices _services;
    private readonly ReshardingOptions _options;
    private readonly ILogger<ReshardingOrchestrator> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Tracks in-flight resharding operations to prevent concurrent resharding.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, ReshardingProgress> _activeOperations = new();

    public ReshardingOrchestrator(
        IShardRebalancer rebalancer,
        IShardTopologyProvider topologyProvider,
        IReshardingStateStore stateStore,
        IReshardingServices services,
        ReshardingOptions options,
        ILogger<ReshardingOrchestrator> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(rebalancer);
        ArgumentNullException.ThrowIfNull(topologyProvider);
        ArgumentNullException.ThrowIfNull(stateStore);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _rebalancer = rebalancer;
        _topologyProvider = topologyProvider;
        _stateStore = stateStore;
        _services = services;
        _options = options;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, ReshardingPlan>> PlanAsync(
        ReshardingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Generating resharding plan.");

        var planningPhase = new PlanningPhase(_rebalancer, _services);
        return await planningPhase.GeneratePlanAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, ReshardingResult>> ExecuteAsync(
        ReshardingPlan plan,
        ReshardingOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(options);

        // Prevent concurrent resharding operations
        if (!_activeOperations.IsEmpty)
        {
            return Either<EncinaError, ReshardingResult>.Left(
                EncinaErrors.Create(
                    ReshardingErrorCodes.ConcurrentReshardingNotAllowed,
                    "A resharding operation is already in progress. Concurrent resharding is not supported."));
        }

        var reshardingId = plan.Id;
        var startedAt = _timeProvider.GetUtcNow().UtcDateTime;

        _logger.LogInformation(
            "Resharding execution starting. ReshardingId={ReshardingId}, Steps={StepCount}, EstimatedRows={EstimatedRows}",
            reshardingId, plan.Steps.Count, plan.Estimate.TotalRows);

        // Capture old topology for rollback support
        var oldTopology = _topologyProvider.GetTopology();

        // Initialize progress
        var initialProgress = new ReshardingProgress(
            reshardingId,
            ReshardingPhase.Copying,
            0.0,
            new Dictionary<string, ShardMigrationProgress>());

        _activeOperations[reshardingId] = initialProgress;

        try
        {
            // Persist initial state for crash recovery
            var initialState = new ReshardingState(
                reshardingId,
                ReshardingPhase.Copying,
                plan,
                initialProgress,
                null,
                startedAt,
                null);

            var saveResult = await _stateStore.SaveStateAsync(initialState, cancellationToken);
            if (saveResult.IsLeft)
            {
                var error = saveResult.Match(Right: _ => default!, Left: e => e);
                _logger.LogError(
                    "Failed to persist initial resharding state. ReshardingId={ReshardingId}",
                    reshardingId);
                return Either<EncinaError, ReshardingResult>.Left(error);
            }

            // Build phase implementations
            var phases = BuildPhases(options);

            // Build shared context
            var context = new PhaseContext(
                reshardingId,
                plan,
                options,
                initialProgress,
                null,
                _services);

            // Execute all phases
            var executor = new ReshardingPhaseExecutor(_stateStore, _logger, _timeProvider);
            var executionResult = await executor.ExecuteAllPhasesAsync(
                initialState, phases, context, options, cancellationToken);

            return await executionResult.MatchAsync(
                RightAsync: async history =>
                {
                    _logger.LogInformation(
                        "Resharding completed successfully. ReshardingId={ReshardingId}, Phases={PhaseCount}",
                        reshardingId, history.Count);

                    // Persist final completed state
                    var completedState = new ReshardingState(
                        reshardingId,
                        ReshardingPhase.Completed,
                        plan,
                        new ReshardingProgress(reshardingId, ReshardingPhase.Completed, 100.0,
                            new Dictionary<string, ShardMigrationProgress>()),
                        ReshardingPhase.CleaningUp,
                        startedAt,
                        null);

                    await _stateStore.SaveStateAsync(completedState, cancellationToken);

                    var result = new ReshardingResult(reshardingId, ReshardingPhase.Completed, history, null);
                    return Either<EncinaError, ReshardingResult>.Right(result);
                },
                Left: error =>
                {
                    _logger.LogError(
                        "Resharding failed. ReshardingId={ReshardingId}, ErrorCode={ErrorCode}",
                        reshardingId, error.GetCode().IfNone("unknown"));

                    // Build a partial result with rollback metadata
                    var rollbackMetadata = new RollbackMetadata(plan, oldTopology, GetLastCompletedPhase(error));
                    var failedResult = new ReshardingResult(
                        reshardingId, ReshardingPhase.Failed, [], rollbackMetadata);
                    return Either<EncinaError, ReshardingResult>.Left(error);
                });
        }
        finally
        {
            _activeOperations.TryRemove(reshardingId, out _);
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> RollbackAsync(
        ReshardingResult result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.RollbackMetadata is null)
        {
            return Either<EncinaError, Unit>.Left(
                EncinaErrors.Create(
                    ReshardingErrorCodes.RollbackNotAvailable,
                    $"No rollback metadata available for resharding operation '{result.Id}'."));
        }

        var metadata = result.RollbackMetadata;

        _logger.LogInformation(
            "Rollback starting. ReshardingId={ReshardingId}, LastCompletedPhase={LastCompletedPhase}",
            result.Id, metadata.LastCompletedPhase);

        // If topology was already swapped (cutover completed), restore original topology
        if (metadata.LastCompletedPhase >= ReshardingPhase.CuttingOver)
        {
            _logger.LogInformation(
                "Restoring original topology. ReshardingId={ReshardingId}",
                result.Id);

            var swapResult = await _services.SwapTopologyAsync(
                metadata.OldTopology, cancellationToken);

            if (swapResult.IsLeft)
            {
                var error = swapResult.Match(Right: _ => default!, Left: e => e);
                _logger.LogError(
                    "Failed to restore original topology during rollback. ReshardingId={ReshardingId}",
                    result.Id);

                return Either<EncinaError, Unit>.Left(
                    EncinaErrors.Create(
                        ReshardingErrorCodes.RollbackFailed,
                        $"Failed to restore original topology for resharding operation '{result.Id}'.",
                        error.MetadataException.MatchUnsafe(e => e, () => null!)));
            }
        }

        // Delete copied data from target shards
        foreach (var step in metadata.OriginalPlan.Steps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cleanupResult = await _services.CleanupSourceDataAsync(
                step.TargetShardId, // Delete from TARGET (rollback)
                step.KeyRange,
                _options.CopyBatchSize,
                cancellationToken);

            if (cleanupResult.IsLeft)
            {
                _logger.LogWarning(
                    "Failed to clean up target shard during rollback. ReshardingId={ReshardingId}, Target={TargetShardId}",
                    result.Id, step.TargetShardId);
                // Continue with remaining steps — best-effort cleanup
            }
        }

        // Update state store to mark as rolled back
        var stateResult = await _stateStore.GetStateAsync(result.Id, cancellationToken);

        if (stateResult.IsRight)
        {
            var state = stateResult.Match(Right: s => s, Left: _ => default!);
            var rolledBackState = new ReshardingState(
                state.Id,
                ReshardingPhase.RolledBack,
                state.Plan,
                state.Progress,
                state.LastCompletedPhase,
                state.StartedAtUtc,
                state.Checkpoint);

            await _stateStore.SaveStateAsync(rolledBackState, cancellationToken);
        }

        _logger.LogInformation(
            "Rollback completed. ReshardingId={ReshardingId}",
            result.Id);

        return Either<EncinaError, Unit>.Right(Unit.Default);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, ReshardingProgress>> GetProgressAsync(
        Guid reshardingId,
        CancellationToken cancellationToken = default)
    {
        // Check in-memory cache first for active operations
        if (_activeOperations.TryGetValue(reshardingId, out var activeProgress))
        {
            return Either<EncinaError, ReshardingProgress>.Right(activeProgress);
        }

        // Fall back to state store
        var stateResult = await _stateStore.GetStateAsync(reshardingId, cancellationToken);

        return stateResult.Match(
            Right: state => Either<EncinaError, ReshardingProgress>.Right(state.Progress),
            Left: _ => Either<EncinaError, ReshardingProgress>.Left(
                EncinaErrors.Create(
                    ReshardingErrorCodes.ReshardingNotFound,
                    $"Resharding operation '{reshardingId}' not found.")));
    }

    private Dictionary<ReshardingPhase, IReshardingPhase> BuildPhases(ReshardingOptions options)
    {
        return new Dictionary<ReshardingPhase, IReshardingPhase>
        {
            [ReshardingPhase.Copying] = new CopyingPhase(_logger),
            [ReshardingPhase.Replicating] = new ReplicatingPhase(_logger),
            [ReshardingPhase.Verifying] = new VerifyingPhase(_logger),
            [ReshardingPhase.CuttingOver] = new CuttingOverPhase(_logger, _topologyProvider),
            [ReshardingPhase.CleaningUp] = new CleaningUpPhase(_logger, _timeProvider),
        };
    }

    private static ReshardingPhase GetLastCompletedPhase(EncinaError error)
    {
        // Try to infer the last completed phase from the error code
        var code = error.GetCode().IfNone(string.Empty);

        return code switch
        {
            ReshardingErrorCodes.CopyFailed => ReshardingPhase.Planning,
            ReshardingErrorCodes.ReplicationFailed => ReshardingPhase.Copying,
            ReshardingErrorCodes.VerificationFailed => ReshardingPhase.Replicating,
            ReshardingErrorCodes.CutoverFailed or ReshardingErrorCodes.CutoverTimeout => ReshardingPhase.Verifying,
            ReshardingErrorCodes.CutoverAborted => ReshardingPhase.Verifying,
            ReshardingErrorCodes.CleanupFailed => ReshardingPhase.CuttingOver,
            _ => ReshardingPhase.Planning,
        };
    }
}
#pragma warning restore CA1848
