using System.Diagnostics;
using Encina.Sharding.Resharding;

namespace Encina.OpenTelemetry.Resharding;

/// <summary>
/// Enriches <see cref="Activity"/> spans with resharding-specific contextual tags.
/// </summary>
/// <remarks>
/// <para>
/// All methods are safe to call with a <see langword="null"/> activity — they
/// short-circuit without allocating when no trace collector is listening.
/// </para>
/// </remarks>
public static class ReshardingActivityEnricher
{
    /// <summary>
    /// Enriches an activity with the overall resharding result metadata.
    /// </summary>
    /// <param name="activity">The activity to enrich (may be <c>null</c>).</param>
    /// <param name="reshardingId">The resharding operation identifier.</param>
    /// <param name="finalPhase">The final phase reached.</param>
    /// <param name="phaseCount">The number of completed phases.</param>
    /// <param name="totalDurationMs">The total duration in milliseconds.</param>
    public static void EnrichWithReshardingResult(
        Activity? activity,
        Guid reshardingId,
        ReshardingPhase finalPhase,
        int phaseCount,
        double totalDurationMs)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(ActivityTagNames.Resharding.Id, reshardingId.ToString());
        activity.SetTag(ActivityTagNames.Resharding.Phase, finalPhase.ToString());
        activity.SetTag("resharding.phase_count", phaseCount);
        activity.SetTag("resharding.total_duration_ms", totalDurationMs);
    }

    /// <summary>
    /// Enriches an activity with shard-level copy progress tags.
    /// </summary>
    /// <param name="activity">The activity to enrich (may be <c>null</c>).</param>
    /// <param name="sourceShardId">The source shard identifier.</param>
    /// <param name="targetShardId">The target shard identifier.</param>
    /// <param name="rowsAffected">The number of rows affected.</param>
    public static void EnrichWithShardCopy(
        Activity? activity,
        string sourceShardId,
        string targetShardId,
        long rowsAffected)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(ActivityTagNames.Resharding.SourceShard, sourceShardId);
        activity.SetTag(ActivityTagNames.Resharding.TargetShard, targetShardId);
        activity.SetTag(ActivityTagNames.Resharding.RowsAffected, rowsAffected);
    }

    /// <summary>
    /// Enriches an activity with rollback metadata.
    /// </summary>
    /// <param name="activity">The activity to enrich (may be <c>null</c>).</param>
    /// <param name="reshardingId">The resharding operation identifier.</param>
    /// <param name="lastCompletedPhase">The last phase completed before failure.</param>
    /// <param name="rollbackDurationMs">The rollback duration in milliseconds.</param>
    public static void EnrichWithRollback(
        Activity? activity,
        Guid reshardingId,
        ReshardingPhase lastCompletedPhase,
        double rollbackDurationMs)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(ActivityTagNames.Resharding.Id, reshardingId.ToString());
        activity.SetTag(ActivityTagNames.Resharding.Phase, lastCompletedPhase.ToString());
        activity.SetTag("resharding.rollback_duration_ms", rollbackDurationMs);
    }
}
