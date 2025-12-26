using Microsoft.Extensions.Logging;

namespace Encina.Marten.Projections;

/// <summary>
/// High-performance logging for projection operations.
/// </summary>
internal static partial class ProjectionLog
{
    // Read Model Repository - Loading
    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Debug,
        Message = "Loading read model {ReadModelType} with ID {Id}")]
    public static partial void LoadingReadModel(
        ILogger logger,
        string readModelType,
        Guid id);

    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Debug,
        Message = "Loaded read model {ReadModelType} with ID {Id}")]
    public static partial void LoadedReadModel(
        ILogger logger,
        string readModelType,
        Guid id);

    [LoggerMessage(
        EventId = 4003,
        Level = LogLevel.Debug,
        Message = "Read model {ReadModelType} with ID {Id} not found")]
    public static partial void ReadModelNotFound(
        ILogger logger,
        string readModelType,
        Guid id);

    [LoggerMessage(
        EventId = 4004,
        Level = LogLevel.Error,
        Message = "Error loading read model {ReadModelType} with ID {Id}")]
    public static partial void ErrorLoadingReadModel(
        ILogger logger,
        Exception exception,
        string readModelType,
        Guid id);

    [LoggerMessage(
        EventId = 4005,
        Level = LogLevel.Debug,
        Message = "Loading {Count} read models of type {ReadModelType}")]
    public static partial void LoadingReadModels(
        ILogger logger,
        string readModelType,
        int count);

    [LoggerMessage(
        EventId = 4006,
        Level = LogLevel.Debug,
        Message = "Loaded {LoadedCount} of {RequestedCount} read models of type {ReadModelType}")]
    public static partial void LoadedReadModels(
        ILogger logger,
        string readModelType,
        int loadedCount,
        int requestedCount);

    [LoggerMessage(
        EventId = 4007,
        Level = LogLevel.Error,
        Message = "Error loading read models of type {ReadModelType}")]
    public static partial void ErrorLoadingReadModels(
        ILogger logger,
        Exception exception,
        string readModelType);

    // Read Model Repository - Querying
    [LoggerMessage(
        EventId = 4010,
        Level = LogLevel.Debug,
        Message = "Querying read models of type {ReadModelType}")]
    public static partial void QueryingReadModels(
        ILogger logger,
        string readModelType);

    [LoggerMessage(
        EventId = 4011,
        Level = LogLevel.Debug,
        Message = "Query returned {Count} read models of type {ReadModelType}")]
    public static partial void QueriedReadModels(
        ILogger logger,
        string readModelType,
        int count);

    [LoggerMessage(
        EventId = 4012,
        Level = LogLevel.Error,
        Message = "Error querying read models of type {ReadModelType}")]
    public static partial void ErrorQueryingReadModels(
        ILogger logger,
        Exception exception,
        string readModelType);

    // Read Model Repository - Storing
    [LoggerMessage(
        EventId = 4020,
        Level = LogLevel.Debug,
        Message = "Storing read model {ReadModelType} with ID {Id}")]
    public static partial void StoringReadModel(
        ILogger logger,
        string readModelType,
        Guid id);

    [LoggerMessage(
        EventId = 4021,
        Level = LogLevel.Debug,
        Message = "Stored read model {ReadModelType} with ID {Id}")]
    public static partial void StoredReadModel(
        ILogger logger,
        string readModelType,
        Guid id);

    [LoggerMessage(
        EventId = 4022,
        Level = LogLevel.Error,
        Message = "Error storing read model {ReadModelType} with ID {Id}")]
    public static partial void ErrorStoringReadModel(
        ILogger logger,
        Exception exception,
        string readModelType,
        Guid id);

    [LoggerMessage(
        EventId = 4023,
        Level = LogLevel.Debug,
        Message = "Storing {Count} read models of type {ReadModelType}")]
    public static partial void StoringReadModels(
        ILogger logger,
        string readModelType,
        int count);

    [LoggerMessage(
        EventId = 4024,
        Level = LogLevel.Debug,
        Message = "Stored {Count} read models of type {ReadModelType}")]
    public static partial void StoredReadModels(
        ILogger logger,
        string readModelType,
        int count);

    [LoggerMessage(
        EventId = 4025,
        Level = LogLevel.Error,
        Message = "Error storing read models of type {ReadModelType}")]
    public static partial void ErrorStoringReadModels(
        ILogger logger,
        Exception exception,
        string readModelType);

    // Read Model Repository - Deleting
    [LoggerMessage(
        EventId = 4030,
        Level = LogLevel.Debug,
        Message = "Deleting read model {ReadModelType} with ID {Id}")]
    public static partial void DeletingReadModel(
        ILogger logger,
        string readModelType,
        Guid id);

    [LoggerMessage(
        EventId = 4031,
        Level = LogLevel.Debug,
        Message = "Deleted read model {ReadModelType} with ID {Id}")]
    public static partial void DeletedReadModel(
        ILogger logger,
        string readModelType,
        Guid id);

    [LoggerMessage(
        EventId = 4032,
        Level = LogLevel.Error,
        Message = "Error deleting read model {ReadModelType} with ID {Id}")]
    public static partial void ErrorDeletingReadModel(
        ILogger logger,
        Exception exception,
        string readModelType,
        Guid id);

    [LoggerMessage(
        EventId = 4033,
        Level = LogLevel.Information,
        Message = "Deleting all read models of type {ReadModelType}")]
    public static partial void DeletingAllReadModels(
        ILogger logger,
        string readModelType);

    [LoggerMessage(
        EventId = 4034,
        Level = LogLevel.Information,
        Message = "Deleted {Count} read models of type {ReadModelType}")]
    public static partial void DeletedAllReadModels(
        ILogger logger,
        string readModelType,
        int count);

    [LoggerMessage(
        EventId = 4035,
        Level = LogLevel.Error,
        Message = "Error deleting all read models of type {ReadModelType}")]
    public static partial void ErrorDeletingAllReadModels(
        ILogger logger,
        Exception exception,
        string readModelType);

    // Projection Manager - Rebuild
    [LoggerMessage(
        EventId = 4050,
        Level = LogLevel.Information,
        Message = "Starting rebuild of projection {ProjectionName}")]
    public static partial void StartingRebuild(
        ILogger logger,
        string projectionName);

    [LoggerMessage(
        EventId = 4051,
        Level = LogLevel.Information,
        Message = "Completed rebuild of projection {ProjectionName}. Processed {EventCount} events")]
    public static partial void CompletedRebuild(
        ILogger logger,
        string projectionName,
        long eventCount);

    [LoggerMessage(
        EventId = 4052,
        Level = LogLevel.Error,
        Message = "Error rebuilding projection {ProjectionName}")]
    public static partial void ErrorRebuild(
        ILogger logger,
        Exception exception,
        string projectionName);

    [LoggerMessage(
        EventId = 4053,
        Level = LogLevel.Debug,
        Message = "Rebuild progress for {ProjectionName}: {ProgressPercent}% ({EventsProcessed} events)")]
    public static partial void RebuildProgress(
        ILogger logger,
        string projectionName,
        int progressPercent,
        long eventsProcessed);

    // Projection Manager - Lifecycle
    [LoggerMessage(
        EventId = 4060,
        Level = LogLevel.Information,
        Message = "Starting projection {ProjectionName}")]
    public static partial void StartingProjection(
        ILogger logger,
        string projectionName);

    [LoggerMessage(
        EventId = 4061,
        Level = LogLevel.Information,
        Message = "Stopped projection {ProjectionName}")]
    public static partial void StoppedProjection(
        ILogger logger,
        string projectionName);

    [LoggerMessage(
        EventId = 4062,
        Level = LogLevel.Information,
        Message = "Paused projection {ProjectionName}")]
    public static partial void PausedProjection(
        ILogger logger,
        string projectionName);

    [LoggerMessage(
        EventId = 4063,
        Level = LogLevel.Information,
        Message = "Resumed projection {ProjectionName}")]
    public static partial void ResumedProjection(
        ILogger logger,
        string projectionName);

    // Inline Projection Dispatcher
    [LoggerMessage(
        EventId = 4070,
        Level = LogLevel.Debug,
        Message = "Dispatching event {EventType} to projection {ProjectionName}")]
    public static partial void DispatchingEvent(
        ILogger logger,
        string eventType,
        string projectionName);

    [LoggerMessage(
        EventId = 4071,
        Level = LogLevel.Debug,
        Message = "Applied event {EventType} to read model {ReadModelType} with ID {Id}")]
    public static partial void AppliedEvent(
        ILogger logger,
        string eventType,
        string readModelType,
        Guid id);

    [LoggerMessage(
        EventId = 4072,
        Level = LogLevel.Debug,
        Message = "Created read model {ReadModelType} with ID {Id} from event {EventType}")]
    public static partial void CreatedReadModel(
        ILogger logger,
        string readModelType,
        Guid id,
        string eventType);

    [LoggerMessage(
        EventId = 4073,
        Level = LogLevel.Debug,
        Message = "Deleted read model {ReadModelType} with ID {Id} from event {EventType}")]
    public static partial void DeletedReadModelFromEvent(
        ILogger logger,
        string readModelType,
        Guid id,
        string eventType);

    [LoggerMessage(
        EventId = 4074,
        Level = LogLevel.Error,
        Message = "Error applying event {EventType} to projection {ProjectionName}")]
    public static partial void ErrorApplyingEvent(
        ILogger logger,
        Exception exception,
        string eventType,
        string projectionName);

    [LoggerMessage(
        EventId = 4075,
        Level = LogLevel.Warning,
        Message = "No handler found for event {EventType} in projection {ProjectionName}")]
    public static partial void NoHandlerForEvent(
        ILogger logger,
        string eventType,
        string projectionName);
}
