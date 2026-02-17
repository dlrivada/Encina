using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Sharding.Resharding.Phases;

/// <summary>
/// Copies data from source shards to target shards in batches with checkpoint persistence.
/// </summary>
#pragma warning disable CA1848 // Use the LoggerMessage delegates
internal sealed class CopyingPhase : IReshardingPhase
{
    private readonly ILogger _logger;

    public CopyingPhase(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public ReshardingPhase Phase => ReshardingPhase.Copying;

    public async Task<Either<EncinaError, PhaseResult>> ExecuteAsync(
        PhaseContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var perStepProgress = new Dictionary<string, ShardMigrationProgress>(
            context.Progress.PerStepProgress);

        long? lastBatchPosition = context.Checkpoint?.LastCopiedBatchPosition;

        foreach (var step in context.Plan.Steps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stepKey = $"{step.SourceShardId}→{step.TargetShardId}";
            var existingProgress = perStepProgress.GetValueOrDefault(stepKey);
            long totalCopied = existingProgress?.RowsCopied ?? 0;

            _logger.LogInformation(
                "Copy step starting. ReshardingId={ReshardingId}, Step={StepKey}, EstimatedRows={EstimatedRows}, ResumeFrom={ResumePosition}",
                context.ReshardingId, stepKey, step.EstimatedRows, lastBatchPosition);

            var hasMore = true;

            while (hasMore)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batchResult = await context.Services.CopyBatchAsync(
                    step.SourceShardId,
                    step.TargetShardId,
                    step.KeyRange,
                    context.Options.CopyBatchSize,
                    lastBatchPosition,
                    cancellationToken);

                if (batchResult.IsLeft)
                {
                    var error = batchResult.Match(Right: _ => default!, Left: e => e);

                    _logger.LogError(
                        "Copy batch failed. ReshardingId={ReshardingId}, Step={StepKey}, RowsCopiedSoFar={RowsCopied}",
                        context.ReshardingId, stepKey, totalCopied);

                    return Either<EncinaError, PhaseResult>.Left(
                        EncinaErrors.Create(
                            ReshardingErrorCodes.CopyFailed,
                            $"Bulk copy failed for step '{stepKey}' after {totalCopied} rows.",
                            error.MetadataException.MatchUnsafe(e => e, () => null!)));
                }

                var result = batchResult.Match(Right: r => r, Left: _ => default!);
                totalCopied += result.RowsCopied;
                lastBatchPosition = result.NewBatchPosition;
                hasMore = result.HasMoreRows;

                // Update progress for checkpoint persistence
                perStepProgress[stepKey] = new ShardMigrationProgress(
                    totalCopied,
                    existingProgress?.RowsReplicated ?? 0,
                    false);
            }

            _logger.LogInformation(
                "Copy step completed. ReshardingId={ReshardingId}, Step={StepKey}, TotalRowsCopied={TotalRowsCopied}",
                context.ReshardingId, stepKey, totalCopied);
        }

        var overallPercent = CalculateOverallPercent(perStepProgress, context.Plan, copyWeight: 0.4);

        var updatedProgress = new ReshardingProgress(
            context.ReshardingId,
            ReshardingPhase.Copying,
            overallPercent,
            perStepProgress);

        var checkpoint = new ReshardingCheckpoint(
            lastBatchPosition,
            context.Checkpoint?.CdcPosition);

        return Either<EncinaError, PhaseResult>.Right(
            new PhaseResult(PhaseStatus.Completed, updatedProgress, checkpoint));
    }

    private static double CalculateOverallPercent(
        Dictionary<string, ShardMigrationProgress> perStep,
        ReshardingPlan plan,
        double copyWeight)
    {
        if (plan.Estimate.TotalRows <= 0)
        {
            return copyWeight * 100;
        }

        var totalCopied = perStep.Values.Sum(p => p.RowsCopied);
        var copyPercent = Math.Min(1.0, (double)totalCopied / plan.Estimate.TotalRows);
        return copyPercent * copyWeight * 100;
    }
}
#pragma warning restore CA1848
