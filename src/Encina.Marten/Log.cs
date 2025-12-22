using Microsoft.Extensions.Logging;

namespace Encina.Marten;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    // Aggregate Repository - Loading (1-4)
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Loading aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void LoadingAggregate(ILogger logger, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Aggregate {AggregateType} with ID {AggregateId} not found")]
    public static partial void AggregateNotFound(ILogger logger, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Loaded aggregate {AggregateType} with ID {AggregateId} at version {Version}")]
    public static partial void LoadedAggregate(ILogger logger, string aggregateType, Guid aggregateId, int version);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Error loading aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void ErrorLoadingAggregate(ILogger logger, Exception exception, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Loading aggregate {AggregateType} with ID {AggregateId} at version {Version}")]
    public static partial void LoadingAggregateAtVersion(ILogger logger, string aggregateType, Guid aggregateId, int version);

    // Aggregate Repository - Saving (10-15)
    [LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = "No uncommitted events for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void NoUncommittedEvents(ILogger logger, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 11, Level = LogLevel.Debug, Message = "Saving {EventCount} events for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void SavingEvents(ILogger logger, int eventCount, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 12, Level = LogLevel.Information, Message = "Saved {EventCount} events for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void SavedEvents(ILogger logger, int eventCount, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 13, Level = LogLevel.Warning, Message = "Concurrency conflict saving aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void ConcurrencyConflict(ILogger logger, Exception exception, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 14, Level = LogLevel.Error, Message = "Error saving aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void ErrorSavingAggregate(ILogger logger, Exception exception, string aggregateType, Guid aggregateId);

    // Aggregate Repository - Creating (20-24)
    [LoggerMessage(EventId = 20, Level = LogLevel.Debug, Message = "Creating aggregate {AggregateType} with ID {AggregateId} with {EventCount} events")]
    public static partial void CreatingAggregate(ILogger logger, string aggregateType, Guid aggregateId, int eventCount);

    [LoggerMessage(EventId = 21, Level = LogLevel.Information, Message = "Created aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void CreatedAggregate(ILogger logger, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 22, Level = LogLevel.Warning, Message = "Stream already exists for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void StreamAlreadyExists(ILogger logger, Exception exception, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 23, Level = LogLevel.Error, Message = "Error creating aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void ErrorCreatingAggregate(ILogger logger, Exception exception, string aggregateType, Guid aggregateId);

    // Event Publishing Pipeline Behavior (30-33)
    [LoggerMessage(EventId = 30, Level = LogLevel.Debug, Message = "Publishing {EventCount} domain events after command {CommandType}")]
    public static partial void PublishingDomainEvents(ILogger logger, int eventCount, string commandType);

    [LoggerMessage(EventId = 31, Level = LogLevel.Error, Message = "Failed to publish domain event {EventType}: {ErrorMessage}")]
    public static partial void FailedToPublishDomainEvent(ILogger logger, string eventType, string errorMessage);

    [LoggerMessage(EventId = 32, Level = LogLevel.Information, Message = "Successfully published {EventCount} domain events after command {CommandType}")]
    public static partial void PublishedDomainEvents(ILogger logger, int eventCount, string commandType);
}
