using Microsoft.Extensions.Logging;

namespace Encina.Marten.Snapshots;

/// <summary>
/// High-performance logging methods for snapshot operations using LoggerMessage source generators.
/// <para>Event IDs: 2629-2649 (see <see cref="Encina.Diagnostics.EventIdRanges.Marten"/>).</para>
/// </summary>
internal static partial class SnapshotLog
{
    // Snapshot Loading
    [LoggerMessage(
        EventId = 2629,
        Level = LogLevel.Debug,
        Message = "Loading latest snapshot for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void LoadingLatestSnapshot(ILogger logger, string aggregateType, Guid aggregateId);

    [LoggerMessage(
        EventId = 2630,
        Level = LogLevel.Debug,
        Message = "Loading snapshot at version {MaxVersion} for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void LoadingSnapshotAtVersion(ILogger logger, string aggregateType, Guid aggregateId, int maxVersion);

    [LoggerMessage(
        EventId = 2631,
        Level = LogLevel.Debug,
        Message = "No snapshot found for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void NoSnapshotFound(ILogger logger, string aggregateType, Guid aggregateId);

    [LoggerMessage(
        EventId = 2632,
        Level = LogLevel.Debug,
        Message = "Loaded snapshot for aggregate {AggregateType} with ID {AggregateId} at version {Version}")]
    public static partial void LoadedSnapshot(ILogger logger, string aggregateType, Guid aggregateId, int version);

    [LoggerMessage(
        EventId = 2633,
        Level = LogLevel.Error,
        Message = "Error loading snapshot for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void ErrorLoadingSnapshot(ILogger logger, Exception exception, string aggregateType, Guid aggregateId);

    // Snapshot Saving
    [LoggerMessage(
        EventId = 2634,
        Level = LogLevel.Debug,
        Message = "Saving snapshot for aggregate {AggregateType} with ID {AggregateId} at version {Version}")]
    public static partial void SavingSnapshot(ILogger logger, string aggregateType, Guid aggregateId, int version);

    [LoggerMessage(
        EventId = 2635,
        Level = LogLevel.Information,
        Message = "Saved snapshot for aggregate {AggregateType} with ID {AggregateId} at version {Version}")]
    public static partial void SavedSnapshot(ILogger logger, string aggregateType, Guid aggregateId, int version);

    [LoggerMessage(
        EventId = 2636,
        Level = LogLevel.Error,
        Message = "Error saving snapshot for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void ErrorSavingSnapshot(ILogger logger, Exception exception, string aggregateType, Guid aggregateId);

    // Snapshot Pruning
    [LoggerMessage(
        EventId = 2637,
        Level = LogLevel.Debug,
        Message = "Pruning snapshots for aggregate {AggregateType} with ID {AggregateId}, keeping {KeepCount}")]
    public static partial void PruningSnapshots(ILogger logger, string aggregateType, Guid aggregateId, int keepCount);

    [LoggerMessage(
        EventId = 2638,
        Level = LogLevel.Information,
        Message = "Pruned {DeletedCount} snapshots for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void PrunedSnapshots(ILogger logger, string aggregateType, Guid aggregateId, int deletedCount);

    [LoggerMessage(
        EventId = 2639,
        Level = LogLevel.Error,
        Message = "Error pruning snapshots for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void ErrorPruningSnapshots(ILogger logger, Exception exception, string aggregateType, Guid aggregateId);

    // Snapshot Deletion
    [LoggerMessage(
        EventId = 2640,
        Level = LogLevel.Debug,
        Message = "Deleting all snapshots for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void DeletingAllSnapshots(ILogger logger, string aggregateType, Guid aggregateId);

    [LoggerMessage(
        EventId = 2641,
        Level = LogLevel.Information,
        Message = "Deleted {DeletedCount} snapshots for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void DeletedAllSnapshots(ILogger logger, string aggregateType, Guid aggregateId, int deletedCount);

    [LoggerMessage(
        EventId = 2642,
        Level = LogLevel.Error,
        Message = "Error deleting snapshots for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void ErrorDeletingSnapshots(ILogger logger, Exception exception, string aggregateType, Guid aggregateId);

    // Snapshot-aware Loading
    [LoggerMessage(
        EventId = 2643,
        Level = LogLevel.Debug,
        Message = "Loading aggregate {AggregateType} with ID {AggregateId} from snapshot at version {SnapshotVersion}")]
    public static partial void LoadingFromSnapshot(ILogger logger, string aggregateType, Guid aggregateId, int snapshotVersion);

    [LoggerMessage(
        EventId = 2644,
        Level = LogLevel.Debug,
        Message = "Replaying {EventCount} events after snapshot for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void ReplayingEventsAfterSnapshot(ILogger logger, int eventCount, string aggregateType, Guid aggregateId);

    [LoggerMessage(
        EventId = 2645,
        Level = LogLevel.Debug,
        Message = "Loaded aggregate {AggregateType} with ID {AggregateId} from snapshot (version {SnapshotVersion}) + {EventCount} events = version {FinalVersion}")]
    public static partial void LoadedFromSnapshotWithEvents(ILogger logger, string aggregateType, Guid aggregateId, int snapshotVersion, int eventCount, int finalVersion);

    // Automatic Snapshot Creation
    [LoggerMessage(
        EventId = 2646,
        Level = LogLevel.Debug,
        Message = "Checking if snapshot should be created for aggregate {AggregateType} with ID {AggregateId} at version {Version}")]
    public static partial void CheckingSnapshotCreation(ILogger logger, string aggregateType, Guid aggregateId, int version);

    [LoggerMessage(
        EventId = 2647,
        Level = LogLevel.Debug,
        Message = "Snapshot threshold reached for aggregate {AggregateType} with ID {AggregateId}: version {Version} >= {Threshold}")]
    public static partial void SnapshotThresholdReached(ILogger logger, string aggregateType, Guid aggregateId, int version, int threshold);

    [LoggerMessage(
        EventId = 2648,
        Level = LogLevel.Debug,
        Message = "Snapshot not needed for aggregate {AggregateType} with ID {AggregateId}: version {Version} < threshold {Threshold}")]
    public static partial void SnapshotNotNeeded(ILogger logger, string aggregateType, Guid aggregateId, int version, int threshold);

    [LoggerMessage(
        EventId = 2649,
        Level = LogLevel.Warning,
        Message = "Error creating automatic snapshot for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void ErrorCreatingAutomaticSnapshot(ILogger logger, Exception exception, string aggregateType, Guid aggregateId);
}
