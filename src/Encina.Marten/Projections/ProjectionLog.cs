using Microsoft.Extensions.Logging;

namespace Encina.Marten.Projections;

/// <summary>
/// High-performance logging for projection operations.
/// <para>Event IDs: 2664-2700 (see <see cref="Encina.Diagnostics.EventIdRanges.Marten"/>).</para>
/// </summary>
internal static partial class ProjectionLog
{
    // Read Model Repository - Loading
    [LoggerMessage(
        EventId = 2664,
        Level = LogLevel.Debug,
        Message = "Loading read model {ReadModelType} with ID {Id}")]
    public static partial void LoadingReadModel(
        ILogger logger,
        string readModelType,
        Guid id);

    [LoggerMessage(
        EventId = 2665,
        Level = LogLevel.Debug,
        Message = "Loaded read model {ReadModelType} with ID {Id}")]
    public static partial void LoadedReadModel(
        ILogger logger,
        string readModelType,
        Guid id);

    [LoggerMessage(
        EventId = 2666,
        Level = LogLevel.Debug,
        Message = "Read model {ReadModelType} with ID {Id} not found")]
    public static partial void ReadModelNotFound(
        ILogger logger,
        string readModelType,
        Guid id);

    [LoggerMessage(
        EventId = 2667,
        Level = LogLevel.Error,
        Message = "Error loading read model {ReadModelType} with ID {Id}")]
    public static partial void ErrorLoadingReadModel(
        ILogger logger,
        Exception exception,
        string readModelType,
        Guid id);

    [LoggerMessage(
        EventId = 2668,
        Level = LogLevel.Debug,
        Message = "Loading {Count} read models of type {ReadModelType}")]
    public static partial void LoadingReadModels(
        ILogger logger,
        string readModelType,
        int count);

    [LoggerMessage(
        EventId = 2669,
        Level = LogLevel.Debug,
        Message = "Loaded {LoadedCount} of {RequestedCount} read models of type {ReadModelType}")]
    public static partial void LoadedReadModels(
        ILogger logger,
        string readModelType,
        int loadedCount,
        int requestedCount);

    [LoggerMessage(
        EventId = 2670,
        Level = LogLevel.Error,
        Message = "Error loading read models of type {ReadModelType}")]
    public static partial void ErrorLoadingReadModels(
        ILogger logger,
        Exception exception,
        string readModelType);

    // Read Model Repository - Querying
    [LoggerMessage(
        EventId = 2671,
        Level = LogLevel.Debug,
        Message = "Querying read models of type {ReadModelType}")]
    public static partial void QueryingReadModels(
        ILogger logger,
        string readModelType);

    [LoggerMessage(
        EventId = 2672,
        Level = LogLevel.Debug,
        Message = "Query returned {Count} read models of type {ReadModelType}")]
    public static partial void QueriedReadModels(
        ILogger logger,
        string readModelType,
        int count);

    [LoggerMessage(
        EventId = 2673,
        Level = LogLevel.Error,
        Message = "Error querying read models of type {ReadModelType}")]
    public static partial void ErrorQueryingReadModels(
        ILogger logger,
        Exception exception,
        string readModelType);

    // Read Model Repository - Storing
    [LoggerMessage(
        EventId = 2674,
        Level = LogLevel.Debug,
        Message = "Storing read model {ReadModelType} with ID {Id}")]
    public static partial void StoringReadModel(
        ILogger logger,
        string readModelType,
        Guid id);

    [LoggerMessage(
        EventId = 2675,
        Level = LogLevel.Debug,
        Message = "Stored read model {ReadModelType} with ID {Id}")]
    public static partial void StoredReadModel(
        ILogger logger,
        string readModelType,
        Guid id);

    [LoggerMessage(
        EventId = 2676,
        Level = LogLevel.Error,
        Message = "Error storing read model {ReadModelType} with ID {Id}")]
    public static partial void ErrorStoringReadModel(
        ILogger logger,
        Exception exception,
        string readModelType,
        Guid id);

    [LoggerMessage(
        EventId = 2677,
        Level = LogLevel.Debug,
        Message = "Storing {Count} read models of type {ReadModelType}")]
    public static partial void StoringReadModels(
        ILogger logger,
        string readModelType,
        int count);

    [LoggerMessage(
        EventId = 2678,
        Level = LogLevel.Debug,
        Message = "Stored {Count} read models of type {ReadModelType}")]
    public static partial void StoredReadModels(
        ILogger logger,
        string readModelType,
        int count);

    [LoggerMessage(
        EventId = 2679,
        Level = LogLevel.Error,
        Message = "Error storing read models of type {ReadModelType}")]
    public static partial void ErrorStoringReadModels(
        ILogger logger,
        Exception exception,
        string readModelType);

    // Read Model Repository - Deleting
    [LoggerMessage(
        EventId = 2680,
        Level = LogLevel.Debug,
        Message = "Deleting read model {ReadModelType} with ID {Id}")]
    public static partial void DeletingReadModel(
        ILogger logger,
        string readModelType,
        Guid id);

    [LoggerMessage(
        EventId = 2681,
        Level = LogLevel.Debug,
        Message = "Deleted read model {ReadModelType} with ID {Id}")]
    public static partial void DeletedReadModel(
        ILogger logger,
        string readModelType,
        Guid id);

    [LoggerMessage(
        EventId = 2682,
        Level = LogLevel.Error,
        Message = "Error deleting read model {ReadModelType} with ID {Id}")]
    public static partial void ErrorDeletingReadModel(
        ILogger logger,
        Exception exception,
        string readModelType,
        Guid id);

    [LoggerMessage(
        EventId = 2683,
        Level = LogLevel.Information,
        Message = "Deleting all read models of type {ReadModelType}")]
    public static partial void DeletingAllReadModels(
        ILogger logger,
        string readModelType);

    [LoggerMessage(
        EventId = 2684,
        Level = LogLevel.Information,
        Message = "Deleted {Count} read models of type {ReadModelType}")]
    public static partial void DeletedAllReadModels(
        ILogger logger,
        string readModelType,
        int count);

    [LoggerMessage(
        EventId = 2685,
        Level = LogLevel.Error,
        Message = "Error deleting all read models of type {ReadModelType}")]
    public static partial void ErrorDeletingAllReadModels(
        ILogger logger,
        Exception exception,
        string readModelType);

    // Projection Manager - Rebuild
    [LoggerMessage(
        EventId = 2686,
        Level = LogLevel.Information,
        Message = "Starting rebuild of projection {ProjectionName}")]
    public static partial void StartingRebuild(
        ILogger logger,
        string projectionName);

    [LoggerMessage(
        EventId = 2687,
        Level = LogLevel.Information,
        Message = "Completed rebuild of projection {ProjectionName}. Processed {EventCount} events")]
    public static partial void CompletedRebuild(
        ILogger logger,
        string projectionName,
        long eventCount);

    [LoggerMessage(
        EventId = 2688,
        Level = LogLevel.Error,
        Message = "Error rebuilding projection {ProjectionName}")]
    public static partial void ErrorRebuild(
        ILogger logger,
        Exception exception,
        string projectionName);

    [LoggerMessage(
        EventId = 2689,
        Level = LogLevel.Debug,
        Message = "Rebuild progress for {ProjectionName}: {ProgressPercent}% ({EventsProcessed} events)")]
    public static partial void RebuildProgress(
        ILogger logger,
        string projectionName,
        int progressPercent,
        long eventsProcessed);

    // Projection Manager - Lifecycle
    [LoggerMessage(
        EventId = 2690,
        Level = LogLevel.Information,
        Message = "Starting projection {ProjectionName}")]
    public static partial void StartingProjection(
        ILogger logger,
        string projectionName);

    [LoggerMessage(
        EventId = 2691,
        Level = LogLevel.Information,
        Message = "Stopped projection {ProjectionName}")]
    public static partial void StoppedProjection(
        ILogger logger,
        string projectionName);

    [LoggerMessage(
        EventId = 2692,
        Level = LogLevel.Information,
        Message = "Paused projection {ProjectionName}")]
    public static partial void PausedProjection(
        ILogger logger,
        string projectionName);

    [LoggerMessage(
        EventId = 2693,
        Level = LogLevel.Information,
        Message = "Resumed projection {ProjectionName}")]
    public static partial void ResumedProjection(
        ILogger logger,
        string projectionName);

    // Inline Projection Dispatcher
    [LoggerMessage(
        EventId = 2694,
        Level = LogLevel.Debug,
        Message = "Dispatching event {EventType} to projection {ProjectionName}")]
    public static partial void DispatchingEvent(
        ILogger logger,
        string eventType,
        string projectionName);

    [LoggerMessage(
        EventId = 2695,
        Level = LogLevel.Debug,
        Message = "Applied event {EventType} to read model {ReadModelType} with ID {Id}")]
    public static partial void AppliedEvent(
        ILogger logger,
        string eventType,
        string readModelType,
        Guid id);

    [LoggerMessage(
        EventId = 2696,
        Level = LogLevel.Debug,
        Message = "Created read model {ReadModelType} with ID {Id} from event {EventType}")]
    public static partial void CreatedReadModel(
        ILogger logger,
        string readModelType,
        Guid id,
        string eventType);

    [LoggerMessage(
        EventId = 2697,
        Level = LogLevel.Debug,
        Message = "Deleted read model {ReadModelType} with ID {Id} from event {EventType}")]
    public static partial void DeletedReadModelFromEvent(
        ILogger logger,
        string readModelType,
        Guid id,
        string eventType);

    [LoggerMessage(
        EventId = 2698,
        Level = LogLevel.Error,
        Message = "Error applying event {EventType} to projection {ProjectionName}")]
    public static partial void ErrorApplyingEvent(
        ILogger logger,
        Exception exception,
        string eventType,
        string projectionName);

    [LoggerMessage(
        EventId = 2699,
        Level = LogLevel.Warning,
        Message = "No handler found for event {EventType} in projection {ProjectionName}")]
    public static partial void NoHandlerForEvent(
        ILogger logger,
        string eventType,
        string projectionName);

    [LoggerMessage(
        EventId = 2700,
        Level = LogLevel.Warning,
        Message = "Inline projections failed for aggregate {AggregateType} with ID {Id} after its events were saved; read models may be stale: {ErrorCode}")]
    public static partial void InlineProjectionFailedAfterSave(
        ILogger logger,
        string aggregateType,
        Guid id,
        string errorCode);
}
