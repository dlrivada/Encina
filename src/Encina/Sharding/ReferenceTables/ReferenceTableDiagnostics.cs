using System.Diagnostics;

namespace Encina.Sharding.ReferenceTables;

/// <summary>
/// Provides the activity source and span helpers for reference table replication tracing.
/// </summary>
internal static class ReferenceTableDiagnostics
{
    internal static readonly ActivitySource ActivitySource = new("Encina.ReferenceTable", "1.0");

    /// <summary>
    /// Starts a span that wraps a full reference table replication operation.
    /// </summary>
    internal static Activity? StartReplicateActivity(string entityType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "encina.reference_table.replicate",
            ActivityKind.Internal);

        activity?.SetTag("reference_table.entity_type", entityType);

        return activity;
    }

    /// <summary>
    /// Starts a span for syncing a single shard (child of the replicate span).
    /// </summary>
    internal static Activity? StartSyncShardActivity(string entityType, string shardId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "encina.reference_table.sync_shard",
            ActivityKind.Internal);

        activity?.SetTag("reference_table.entity_type", entityType);
        activity?.SetTag("reference_table.shard_id", shardId);

        return activity;
    }

    /// <summary>
    /// Starts a span for polling-based change detection (hash comparison).
    /// </summary>
    internal static Activity? StartDetectChangesActivity(string entityType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "encina.reference_table.detect_changes",
            ActivityKind.Internal);

        activity?.SetTag("reference_table.entity_type", entityType);

        return activity;
    }

    /// <summary>
    /// Completes an activity span with success or error status.
    /// </summary>
    internal static void Complete(Activity? activity, bool success, string? errorMessage = null)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetStatus(
            success ? ActivityStatusCode.Ok : ActivityStatusCode.Error,
            errorMessage);

        activity.Dispose();
    }
}
