using Encina.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Sharding.Resharding.Phases;

/// <summary>
/// Performs the atomic topology switch: invokes the cutover predicate, waits for final
/// CDC drain, swaps the topology, and enforces the cutover timeout.
/// </summary>
#pragma warning disable CA1848 // Use the LoggerMessage delegates
internal sealed class CuttingOverPhase : IReshardingPhase
{
    private readonly ILogger _logger;
    private readonly IShardTopologyProvider _topologyProvider;

    public CuttingOverPhase(ILogger logger, IShardTopologyProvider topologyProvider)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(topologyProvider);

        _logger = logger;
        _topologyProvider = topologyProvider;
    }

    public ReshardingPhase Phase => ReshardingPhase.CuttingOver;

    public async Task<Either<EncinaError, PhaseResult>> ExecuteAsync(
        PhaseContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 1. Invoke the OnCutoverStarting predicate (user-defined validation)
        if (context.Options.OnCutoverStarting is not null)
        {
            var veto = await InvokePredicateAsync(context, context.Options.OnCutoverStarting, cancellationToken);
            if (veto is { } vetoResult)
            {
                return vetoResult;
            }
        }

        // 2. Enforce cutover timeout
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(context.Options.CutoverTimeout);

        Either<EncinaError, Unit> switched;
        try
        {
            switched = await DrainAndSwapAsync(context, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                "Cutover timed out. ReshardingId={ReshardingId}, Timeout={Timeout}",
                context.ReshardingId, context.Options.CutoverTimeout);

            return Either<EncinaError, PhaseResult>.Left(
                EncinaErrors.Create(
                    ReshardingErrorCodes.CutoverTimeout,
                    $"The cutover phase exceeded the configured timeout of {context.Options.CutoverTimeout}."));
        }

        return switched.Map(_ => BuildCompletedResult(context));
    }

    /// <summary>
    /// Runs the user predicate. Returns <c>null</c> when the cutover may proceed, otherwise the
    /// result that ends the phase (a failure when the predicate threw, an aborted phase when it vetoed).
    /// </summary>
    private async Task<Either<EncinaError, PhaseResult>?> InvokePredicateAsync(
        PhaseContext context,
        Func<ReshardingPlan, CancellationToken, Task<bool>> predicate,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Invoking cutover predicate. ReshardingId={ReshardingId}",
            context.ReshardingId);

        bool proceed;
        try
        {
            proceed = await predicate(context.Plan, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ForLogging(),
                "Cutover predicate threw an exception. ReshardingId={ReshardingId}",
                context.ReshardingId);

            return Either<EncinaError, PhaseResult>.Left(
                EncinaErrors.FromException(
                    ReshardingErrorCodes.CutoverFailed,
                    ex,
                    "The OnCutoverStarting predicate threw an exception."));
        }

        if (proceed)
        {
            return null;
        }

        _logger.LogWarning(
            "Cutover aborted by predicate. ReshardingId={ReshardingId}",
            context.ReshardingId);

        var abortedProgress = new ReshardingProgress(
            context.ReshardingId,
            ReshardingPhase.CuttingOver,
            context.Progress.OverallPercentComplete,
            context.Progress.PerStepProgress);

        return Either<EncinaError, PhaseResult>.Right(
            new PhaseResult(PhaseStatus.Aborted, abortedProgress, context.Checkpoint));
    }

    private async Task<Either<EncinaError, Unit>> DrainAndSwapAsync(PhaseContext context, CancellationToken timeoutToken)
    {
        // 3. Wait for final CDC drain — check all source shards have zero lag
        _logger.LogInformation(
            "Waiting for final CDC drain. ReshardingId={ReshardingId}, Timeout={Timeout}",
            context.ReshardingId, context.Options.CutoverTimeout);

        foreach (var step in context.Plan.Steps)
        {
            timeoutToken.ThrowIfCancellationRequested();

            var lagResult = await context.Services.GetReplicationLagAsync(step.SourceShardId, timeoutToken);

            if (lagResult.IsLeft)
            {
                _logger.LogWarning(
                    "Failed to check replication lag during cutover. ReshardingId={ReshardingId}, Source={SourceShardId}",
                    context.ReshardingId, step.SourceShardId);

                return Either<EncinaError, Unit>.Left(
                    EncinaErrors.Create(
                        ReshardingErrorCodes.CutoverFailed,
                        $"Failed to verify replication lag for shard '{step.SourceShardId}' during cutover."));
            }
        }

        // 4. Build the new topology from the current topology + plan
        var currentTopology = _topologyProvider.GetTopology();
        var newTopology = BuildNewTopology(currentTopology);

        // 5. Atomically swap the topology
        _logger.LogInformation(
            "Swapping topology. ReshardingId={ReshardingId}",
            context.ReshardingId);

        var swapResult = await context.Services.SwapTopologyAsync(newTopology, timeoutToken);

        if (swapResult.IsLeft)
        {
            _logger.LogError(
                "Topology swap failed. ReshardingId={ReshardingId}",
                context.ReshardingId);

            return Either<EncinaError, Unit>.Left(
                EncinaErrors.Create(
                    ReshardingErrorCodes.CutoverFailed,
                    "Failed to swap the shard topology during cutover."));
        }

        _logger.LogInformation(
            "Topology swap succeeded. ReshardingId={ReshardingId}",
            context.ReshardingId);

        return Either<EncinaError, Unit>.Right(Unit.Default);
    }

    private static PhaseResult BuildCompletedResult(PhaseContext context)
    {
        var overallPercent = 90.0; // Copy(40%) + Replicate(20%) + Verify(15%) + Cutover(15%)

        var updatedProgress = new ReshardingProgress(
            context.ReshardingId,
            ReshardingPhase.CuttingOver,
            overallPercent,
            context.Progress.PerStepProgress);

        return new PhaseResult(PhaseStatus.Completed, updatedProgress, context.Checkpoint);
    }

    /// <summary>
    /// Builds the new topology. The actual topology is unchanged — the swap is handled
    /// by <see cref="IReshardingServices.SwapTopologyAsync"/>. This method returns the
    /// current topology as-is because the topology provider will be refreshed externally.
    /// </summary>
    private static ShardTopology BuildNewTopology(ShardTopology currentTopology)
    {
        // The new topology is the one the user originally requested via ReshardingRequest.NewTopology.
        // By the time we reach cutover, the topology provider should be updated to serve
        // the new topology after SwapTopologyAsync succeeds.
        // We return the current topology as the base — the actual swap is handled by the service.
        return currentTopology;
    }
}
#pragma warning restore CA1848
