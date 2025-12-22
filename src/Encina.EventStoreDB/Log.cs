using Microsoft.Extensions.Logging;

namespace Encina.EventStoreDB;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Loading aggregate {AggregateType} with ID {AggregateId} from stream {StreamName}")]
    public static partial void LoadingAggregate(ILogger logger, string aggregateType, Guid aggregateId, string streamName);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Aggregate {AggregateType} with ID {AggregateId} not found (empty stream)")]
    public static partial void AggregateNotFoundEmpty(ILogger logger, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Loaded aggregate {AggregateType} with ID {AggregateId} at version {Version} ({EventCount} events)")]
    public static partial void LoadedAggregate(ILogger logger, string aggregateType, Guid aggregateId, long version, long eventCount);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Stream {StreamName} not found for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void StreamNotFound(ILogger logger, string streamName, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Error loading aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void ErrorLoadingAggregate(ILogger logger, Exception exception, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "Loading aggregate {AggregateType} with ID {AggregateId} at version {Version}")]
    public static partial void LoadingAggregateAtVersion(ILogger logger, string aggregateType, Guid aggregateId, long version);

    [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = "No uncommitted events for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void NoUncommittedEvents(ILogger logger, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "Saving {EventCount} events for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void SavingEvents(ILogger logger, int eventCount, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "Saved {EventCount} events for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void SavedEvents(ILogger logger, int eventCount, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 10, Level = LogLevel.Warning, Message = "Concurrency conflict saving aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void ConcurrencyConflict(ILogger logger, Exception exception, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 11, Level = LogLevel.Error, Message = "Error saving aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void ErrorSavingAggregate(ILogger logger, Exception exception, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 12, Level = LogLevel.Debug, Message = "Creating aggregate {AggregateType} with ID {AggregateId} with {EventCount} events")]
    public static partial void CreatingAggregate(ILogger logger, string aggregateType, Guid aggregateId, int eventCount);

    [LoggerMessage(EventId = 13, Level = LogLevel.Information, Message = "Created aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void CreatedAggregate(ILogger logger, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 14, Level = LogLevel.Warning, Message = "Stream already exists for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void StreamAlreadyExists(ILogger logger, Exception exception, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 15, Level = LogLevel.Error, Message = "Error creating aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void ErrorCreatingAggregate(ILogger logger, Exception exception, string aggregateType, Guid aggregateId);
}
