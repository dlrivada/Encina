using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Sharding.Resharding.Phases;

/// <summary>
/// Streams CDC changes from source shards and applies them to target shards
/// until replication lag falls below the configured threshold.
/// </summary>
#pragma warning disable CA1848 // Use the LoggerMessage delegates
internal sealed class ReplicatingPhase : IReshardingPhase
{
    private readonly ILogger _logger;

    public ReplicatingPhase(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public ReshardingPhase Phase => ReshardingPhase.Replicating;

    public async Task<Either<EncinaError, PhaseResult>> ExecuteAsync(
        PhaseContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var perStepProgress = new Dictionary<string, ShardMigrationProgress>(
            context.Progress.PerStepProgress);

        string? latestCdcPosition = context.Checkpoint?.CdcPosition;

        foreach (var step in context.Plan.Steps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stepKey = $"{step.SourceShardId}→{step.TargetShardId}";
            var existingProgress = perStepProgress.GetValueOrDefault(stepKey);

            _logger.LogInformation(
                "Replication step starting. ReshardingId={ReshardingId}, Step={StepKey}, CdcPosition={CdcPosition}",
                context.ReshardingId, stepKey, latestCdcPosition);

            var totalReplicated = existingProgress?.RowsReplicated ?? 0;
            var lagBelowThreshold = false;

            while (!lagBelowThreshold)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var replicationResult = await context.Services.ReplicateChangesAsync(
                    step.SourceShardId,
                    step.TargetShardId,
                    step.KeyRange,
                    latestCdcPosition,
                    cancellationToken);

                if (replicationResult.IsLeft)
                {
                    var error = replicationResult.Match(Right: _ => default!, Left: e => e);

                    _logger.LogError(
                        "Replication failed. ReshardingId={ReshardingId}, Step={StepKey}, RowsReplicatedSoFar={RowsReplicated}",
                        context.ReshardingId, stepKey, totalReplicated);

                    return Either<EncinaError, PhaseResult>.Left(
                        EncinaErrors.Create(
                            ReshardingErrorCodes.ReplicationFailed,
                            $"CDC replication failed for step '{stepKey}' after {totalReplicated} rows.",
                            error.MetadataException.MatchUnsafe(e => e, () => null!)));
                }

                var result = replicationResult.Match(Right: r => r, Left: _ => default!);
                totalReplicated += result.RowsReplicated;
                latestCdcPosition = result.FinalCdcPosition ?? latestCdcPosition;

                // Check if lag is below threshold
                if (result.CurrentLag <= context.Options.CdcLagThreshold && result.RowsReplicated == 0)
                {
                    lagBelowThreshold = true;
                }

                _logger.LogDebug(
                    "Replication pass completed. ReshardingId={ReshardingId}, Step={StepKey}, RowsThisPass={RowsThisPass}, CurrentLag={CurrentLag}, BelowThreshold={BelowThreshold}",
                    context.ReshardingId, stepKey, result.RowsReplicated, result.CurrentLag, lagBelowThreshold);
            }

            perStepProgress[stepKey] = new ShardMigrationProgress(
                existingProgress?.RowsCopied ?? 0,
                totalReplicated,
                false);

            _logger.LogInformation(
                "Replication step completed. ReshardingId={ReshardingId}, Step={StepKey}, TotalRowsReplicated={TotalReplicated}",
                context.ReshardingId, stepKey, totalReplicated);
        }

        var overallPercent = 60.0; // Copy(40%) + Replicate(20%)

        var updatedProgress = new ReshardingProgress(
            context.ReshardingId,
            ReshardingPhase.Replicating,
            overallPercent,
            perStepProgress);

        var checkpoint = new ReshardingCheckpoint(
            context.Checkpoint?.LastCopiedBatchPosition,
            latestCdcPosition);

        return Either<EncinaError, PhaseResult>.Right(
            new PhaseResult(PhaseStatus.Completed, updatedProgress, checkpoint));
    }
}
#pragma warning restore CA1848
