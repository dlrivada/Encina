using Encina.Sharding.Routing;
using LanguageExt;

namespace Encina.Sharding.Resharding.Phases;

/// <summary>
/// Generates a resharding plan by analyzing topology differences and estimating row counts.
/// </summary>
internal sealed class PlanningPhase : IReshardingPhase
{
    private readonly IShardRebalancer _rebalancer;
    private readonly IReshardingServices _services;

    public PlanningPhase(IShardRebalancer rebalancer, IReshardingServices services)
    {
        ArgumentNullException.ThrowIfNull(rebalancer);
        ArgumentNullException.ThrowIfNull(services);

        _rebalancer = rebalancer;
        _services = services;
    }

    public ReshardingPhase Phase => ReshardingPhase.Planning;

    /// <summary>
    /// Generates a <see cref="ReshardingPlan"/> from the affected key ranges.
    /// </summary>
    /// <remarks>
    /// This phase is special — it is called via <see cref="IReshardingOrchestrator.PlanAsync"/>
    /// rather than through the phase executor.
    /// </remarks>
    public async Task<Either<EncinaError, ReshardingPlan>> GeneratePlanAsync(
        ReshardingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        IReadOnlyList<AffectedKeyRange> affectedRanges;

        try
        {
            affectedRanges = _rebalancer.CalculateAffectedKeyRanges(
                request.OldTopology, request.NewTopology);
        }
        catch (Exception ex)
        {
            return Either<EncinaError, ReshardingPlan>.Left(
                EncinaErrors.FromException(
                    ReshardingErrorCodes.PlanGenerationFailed,
                    ex,
                    "Failed to calculate affected key ranges."));
        }

        if (affectedRanges.Count == 0)
        {
            return Either<EncinaError, ReshardingPlan>.Left(
                EncinaErrors.Create(
                    ReshardingErrorCodes.TopologiesIdentical,
                    "The old and new topologies are identical — no resharding is needed."));
        }

        var steps = new List<ShardMigrationStep>();
        long totalRows = 0;
        long totalBytes = 0;

        foreach (var range in affectedRanges)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var keyRange = new KeyRange(range.RingStart, range.RingEnd);

            var estimateResult = await _services.EstimateRowCountAsync(
                range.PreviousShardId, keyRange, cancellationToken);

            var estimatedRows = estimateResult.Match(Right: r => r, Left: _ => 0L);

            steps.Add(new ShardMigrationStep(
                range.PreviousShardId,
                range.NewShardId,
                keyRange,
                estimatedRows));

            totalRows += estimatedRows;
            // Rough estimate: 256 bytes per row average
            totalBytes += estimatedRows * 256;
        }

        if (steps.Count == 0)
        {
            return Either<EncinaError, ReshardingPlan>.Left(
                EncinaErrors.Create(
                    ReshardingErrorCodes.EmptyPlan,
                    "The resharding plan contains no migration steps."));
        }

        // Estimate duration: ~10,000 rows/sec for bulk copy + overhead
        var estimatedSeconds = Math.Max(1, totalRows / 10_000.0) * 2; // 2x for copy + replicate + verify
        var estimate = new EstimatedResources(totalRows, totalBytes, TimeSpan.FromSeconds(estimatedSeconds));

        var plan = new ReshardingPlan(Guid.NewGuid(), steps, estimate);

        return Either<EncinaError, ReshardingPlan>.Right(plan);
    }

    /// <inheritdoc />
    Task<Either<EncinaError, PhaseResult>> IReshardingPhase.ExecuteAsync(
        PhaseContext context,
        CancellationToken cancellationToken)
    {
        // Planning is handled via GeneratePlanAsync, not through the phase executor.
        return Task.FromResult(
            Either<EncinaError, PhaseResult>.Left(
                EncinaErrors.Create(
                    ReshardingErrorCodes.InvalidPhaseTransition,
                    "PlanningPhase should not be executed through the phase executor.")));
    }
}
