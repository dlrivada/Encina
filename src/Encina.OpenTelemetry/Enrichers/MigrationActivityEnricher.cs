using System.Diagnostics;
using Encina.Sharding.Migrations;

namespace Encina.OpenTelemetry.Enrichers;

/// <summary>
/// Enriches OpenTelemetry activities with shard migration coordination information.
/// </summary>
/// <remarks>
/// All methods are null-safe — if the activity is <c>null</c>, the methods return immediately.
/// </remarks>
public static class MigrationActivityEnricher
{
    /// <summary>
    /// Enriches an activity with migration coordination result information.
    /// </summary>
    /// <param name="activity">The activity to enrich (may be <c>null</c>).</param>
    /// <param name="migrationId">The migration execution identifier.</param>
    /// <param name="strategy">The migration strategy used.</param>
    /// <param name="totalShards">The total number of shards in the migration.</param>
    /// <param name="succeededCount">The number of shards that succeeded.</param>
    /// <param name="failedCount">The number of shards that failed.</param>
    /// <param name="totalDurationMs">The total duration of the migration in milliseconds.</param>
    public static void EnrichWithMigrationResult(
        Activity? activity,
        Guid migrationId,
        MigrationStrategy strategy,
        int totalShards,
        int succeededCount,
        int failedCount,
        double totalDurationMs)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(ActivityTagNames.Migration.Id, migrationId.ToString());
        activity.SetTag(ActivityTagNames.Migration.Strategy, strategy.ToString());
        activity.SetTag(ActivityTagNames.Migration.ShardCount, totalShards);
        activity.SetTag(ActivityTagNames.Migration.ShardsSucceeded, succeededCount);
        activity.SetTag(ActivityTagNames.Migration.ShardsFailed, failedCount);
        activity.SetTag(ActivityTagNames.Migration.DurationMs, totalDurationMs);
    }

    /// <summary>
    /// Enriches an activity with schema drift detection information.
    /// </summary>
    /// <param name="activity">The activity to enrich (may be <c>null</c>).</param>
    /// <param name="hasDrift">Whether any schema drift was detected.</param>
    /// <param name="driftedShardCount">The number of shards with drift.</param>
    /// <param name="baselineShardId">The baseline shard used for comparison.</param>
    public static void EnrichWithDriftDetection(
        Activity? activity,
        bool hasDrift,
        int driftedShardCount,
        string? baselineShardId)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(ActivityTagNames.Migration.DriftDetected, hasDrift);
        activity.SetTag(ActivityTagNames.Migration.DriftedShardCount, driftedShardCount);

        if (!string.IsNullOrWhiteSpace(baselineShardId))
        {
            activity.SetTag(ActivityTagNames.Migration.BaselineShardId, baselineShardId);
        }
    }

    /// <summary>
    /// Enriches an activity with rollback information.
    /// </summary>
    /// <param name="activity">The activity to enrich (may be <c>null</c>).</param>
    /// <param name="migrationId">The migration execution identifier.</param>
    /// <param name="shardsRolledBack">The number of shards rolled back.</param>
    /// <param name="rollbackDurationMs">The total rollback duration in milliseconds.</param>
    public static void EnrichWithRollback(
        Activity? activity,
        Guid migrationId,
        int shardsRolledBack,
        double rollbackDurationMs)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(ActivityTagNames.Migration.Id, migrationId.ToString());
        activity.SetTag(ActivityTagNames.Migration.ShardsRolledBack, shardsRolledBack);
        activity.SetTag(ActivityTagNames.Migration.RollbackDurationMs, rollbackDurationMs);
    }
}
