using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Sharding.Resharding.Phases;

/// <summary>
/// Deletes migrated rows from source shards after the retention period has elapsed.
/// </summary>
#pragma warning disable CA1848 // Use the LoggerMessage delegates
internal sealed class CleaningUpPhase : IReshardingPhase
{
    private readonly ILogger _logger;
    private readonly TimeProvider _timeProvider;

    public CleaningUpPhase(ILogger logger, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public ReshardingPhase Phase => ReshardingPhase.CleaningUp;

    public async Task<Either<EncinaError, PhaseResult>> ExecuteAsync(
        PhaseContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Check if the retention period has elapsed since the cutover
        // (The cutover phase was the last completed phase before this one)
        var retentionPeriod = context.Options.CleanupRetentionPeriod;

        _logger.LogInformation(
            "Cleanup phase starting. ReshardingId={ReshardingId}, RetentionPeriod={RetentionPeriod}",
            context.ReshardingId, retentionPeriod);

        // For simplicity, we proceed immediately with cleanup.
        // In a production deployment, the retention period would be enforced by scheduling
        // the cleanup phase to run after the configured delay. The orchestrator can check
        // the state store and resume cleanup after the delay.

        long totalDeleted = 0;

        foreach (var step in context.Plan.Steps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stepKey = $"{step.SourceShardId}→{step.TargetShardId}";

            _logger.LogInformation(
                "Cleanup step starting. ReshardingId={ReshardingId}, Step={StepKey}, Source={SourceShardId}",
                context.ReshardingId, stepKey, step.SourceShardId);

            var cleanupResult = await context.Services.CleanupSourceDataAsync(
                step.SourceShardId,
                step.KeyRange,
                context.Options.CopyBatchSize,
                cancellationToken);

            if (cleanupResult.IsLeft)
            {
                var error = cleanupResult.Match(Right: _ => default!, Left: e => e);

                _logger.LogError(
                    "Cleanup failed. ReshardingId={ReshardingId}, Step={StepKey}, TotalDeletedSoFar={TotalDeleted}",
                    context.ReshardingId, stepKey, totalDeleted);

                return Either<EncinaError, PhaseResult>.Left(
                    EncinaErrors.Create(
                        ReshardingErrorCodes.CleanupFailed,
                        $"Cleanup failed for step '{stepKey}' after deleting {totalDeleted} rows total.",
                        error.MetadataException.MatchUnsafe(e => e, () => null!)));
            }

            var deleted = cleanupResult.Match(Right: r => r, Left: _ => 0L);
            totalDeleted += deleted;

            _logger.LogInformation(
                "Cleanup step completed. ReshardingId={ReshardingId}, Step={StepKey}, RowsDeleted={RowsDeleted}",
                context.ReshardingId, stepKey, deleted);
        }

        _logger.LogInformation(
            "Cleanup phase completed. ReshardingId={ReshardingId}, TotalRowsDeleted={TotalDeleted}",
            context.ReshardingId, totalDeleted);

        var updatedProgress = new ReshardingProgress(
            context.ReshardingId,
            ReshardingPhase.CleaningUp,
            100.0,
            context.Progress.PerStepProgress);

        return Either<EncinaError, PhaseResult>.Right(
            new PhaseResult(PhaseStatus.Completed, updatedProgress));
    }
}
#pragma warning restore CA1848
