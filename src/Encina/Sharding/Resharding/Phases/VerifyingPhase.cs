using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Sharding.Resharding.Phases;

/// <summary>
/// Verifies data consistency between source and target shards using the configured
/// verification mode (count, checksum, or both).
/// </summary>
#pragma warning disable CA1848 // Use the LoggerMessage delegates
internal sealed class VerifyingPhase : IReshardingPhase
{
    private readonly ILogger _logger;

    public VerifyingPhase(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public ReshardingPhase Phase => ReshardingPhase.Verifying;

    public async Task<Either<EncinaError, PhaseResult>> ExecuteAsync(
        PhaseContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var perStepProgress = new Dictionary<string, ShardMigrationProgress>(
            context.Progress.PerStepProgress);

        foreach (var step in context.Plan.Steps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stepKey = $"{step.SourceShardId}→{step.TargetShardId}";
            var existingProgress = perStepProgress.GetValueOrDefault(stepKey);

            _logger.LogInformation(
                "Verification step starting. ReshardingId={ReshardingId}, Step={StepKey}, Mode={Mode}",
                context.ReshardingId, stepKey, context.Options.VerificationMode);

            var verifyResult = await context.Services.VerifyDataConsistencyAsync(
                step.SourceShardId,
                step.TargetShardId,
                step.KeyRange,
                context.Options.VerificationMode,
                cancellationToken);

            if (verifyResult.IsLeft)
            {
                var error = verifyResult.Match(Right: _ => default!, Left: e => e);

                _logger.LogError(
                    "Verification infrastructure failed. ReshardingId={ReshardingId}, Step={StepKey}",
                    context.ReshardingId, stepKey);

                return Either<EncinaError, PhaseResult>.Left(
                    EncinaErrors.Create(
                        ReshardingErrorCodes.VerificationFailed,
                        $"Verification infrastructure failed for step '{stepKey}'.",
                        error.MetadataException.MatchUnsafe(e => e, () => null!)));
            }

            var result = verifyResult.Match(Right: r => r, Left: _ => default!);

            if (!result.IsConsistent)
            {
                _logger.LogError(
                    "Verification mismatch detected. ReshardingId={ReshardingId}, Step={StepKey}, SourceRows={SourceRows}, TargetRows={TargetRows}, Details={MismatchDetails}",
                    context.ReshardingId, stepKey, result.SourceRowCount, result.TargetRowCount, result.MismatchDetails);

                return Either<EncinaError, PhaseResult>.Left(
                    EncinaErrors.Create(
                        ReshardingErrorCodes.VerificationFailed,
                        $"Data verification failed for step '{stepKey}': " +
                        $"source has {result.SourceRowCount} rows, target has {result.TargetRowCount} rows. " +
                        $"{result.MismatchDetails ?? "No additional details."}"));
            }

            perStepProgress[stepKey] = new ShardMigrationProgress(
                existingProgress?.RowsCopied ?? 0,
                existingProgress?.RowsReplicated ?? 0,
                true);

            _logger.LogInformation(
                "Verification step passed. ReshardingId={ReshardingId}, Step={StepKey}, SourceRows={SourceRows}, TargetRows={TargetRows}",
                context.ReshardingId, stepKey, result.SourceRowCount, result.TargetRowCount);
        }

        var overallPercent = 75.0; // Copy(40%) + Replicate(20%) + Verify(15%)

        var updatedProgress = new ReshardingProgress(
            context.ReshardingId,
            ReshardingPhase.Verifying,
            overallPercent,
            perStepProgress);

        return Either<EncinaError, PhaseResult>.Right(
            new PhaseResult(PhaseStatus.Completed, updatedProgress, context.Checkpoint));
    }
}
#pragma warning restore CA1848
