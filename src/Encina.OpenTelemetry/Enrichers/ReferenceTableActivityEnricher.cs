using System.Diagnostics;

namespace Encina.OpenTelemetry.Enrichers;

/// <summary>
/// Enriches OpenTelemetry activities with reference table replication information.
/// </summary>
/// <remarks>
/// All methods are null-safe — if the activity is <c>null</c>, the methods return immediately.
/// </remarks>
public static class ReferenceTableActivityEnricher
{
    /// <summary>
    /// Enriches an activity with reference table replication result information.
    /// </summary>
    /// <param name="activity">The activity to enrich (may be <c>null</c>).</param>
    /// <param name="entityType">The entity type name of the reference table.</param>
    /// <param name="rowsSynced">The total number of rows synced.</param>
    /// <param name="shardCount">The number of shards that were replicated to.</param>
    /// <param name="durationMs">The replication duration in milliseconds.</param>
    public static void EnrichWithReplication(
        Activity? activity,
        string entityType,
        int rowsSynced,
        int shardCount,
        double durationMs)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(ActivityTagNames.ReferenceTable.EntityType, entityType);
        activity.SetTag(ActivityTagNames.ReferenceTable.RowsSynced, rowsSynced);
        activity.SetTag(ActivityTagNames.ReferenceTable.ShardCount, shardCount);
        activity.SetTag(ActivityTagNames.ReferenceTable.DurationMs, durationMs);
    }

    /// <summary>
    /// Enriches an activity with reference table change detection information.
    /// </summary>
    /// <param name="activity">The activity to enrich (may be <c>null</c>).</param>
    /// <param name="entityType">The entity type name of the reference table.</param>
    /// <param name="changeDetected">Whether a change was detected.</param>
    /// <param name="hash">The content hash value (may be <c>null</c>).</param>
    public static void EnrichWithChangeDetection(
        Activity? activity,
        string entityType,
        bool changeDetected,
        string? hash)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(ActivityTagNames.ReferenceTable.EntityType, entityType);
        activity.SetTag(ActivityTagNames.ReferenceTable.ChangeDetected, changeDetected);

        if (hash is not null)
        {
            activity.SetTag(ActivityTagNames.ReferenceTable.HashValue, hash);
        }
    }
}
