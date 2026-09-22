using Microsoft.Extensions.Logging;

namespace Encina.Marten;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// <para>Event IDs: 2600-2616 (see <see cref="Encina.Diagnostics.EventIdRanges.Marten"/>).</para>
/// </summary>
internal static partial class Log
{
    // Aggregate Repository - Loading
    [LoggerMessage(EventId = 2600, Level = LogLevel.Debug, Message = "Loading aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void LoadingAggregate(ILogger logger, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 2601, Level = LogLevel.Warning, Message = "Aggregate {AggregateType} with ID {AggregateId} not found")]
    public static partial void AggregateNotFound(ILogger logger, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 2602, Level = LogLevel.Debug, Message = "Loaded aggregate {AggregateType} with ID {AggregateId} at version {Version}")]
    public static partial void LoadedAggregate(ILogger logger, string aggregateType, Guid aggregateId, int version);

    [LoggerMessage(EventId = 2603, Level = LogLevel.Error, Message = "Error loading aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void ErrorLoadingAggregate(ILogger logger, Exception exception, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 2604, Level = LogLevel.Debug, Message = "Loading aggregate {AggregateType} with ID {AggregateId} at version {Version}")]
    public static partial void LoadingAggregateAtVersion(ILogger logger, string aggregateType, Guid aggregateId, int version);

    // Aggregate Repository - Saving
    [LoggerMessage(EventId = 2605, Level = LogLevel.Debug, Message = "No uncommitted events for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void NoUncommittedEvents(ILogger logger, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 2606, Level = LogLevel.Debug, Message = "Saving {EventCount} events for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void SavingEvents(ILogger logger, int eventCount, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 2607, Level = LogLevel.Information, Message = "Saved {EventCount} events for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void SavedEvents(ILogger logger, int eventCount, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 2608, Level = LogLevel.Warning, Message = "Concurrency conflict saving aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void ConcurrencyConflict(ILogger logger, Exception exception, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 2609, Level = LogLevel.Error, Message = "Error saving aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void ErrorSavingAggregate(ILogger logger, Exception exception, string aggregateType, Guid aggregateId);

    // Aggregate Repository - Creating
    [LoggerMessage(EventId = 2610, Level = LogLevel.Debug, Message = "Creating aggregate {AggregateType} with ID {AggregateId} with {EventCount} events")]
    public static partial void CreatingAggregate(ILogger logger, string aggregateType, Guid aggregateId, int eventCount);

    [LoggerMessage(EventId = 2611, Level = LogLevel.Information, Message = "Created aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void CreatedAggregate(ILogger logger, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 2612, Level = LogLevel.Warning, Message = "Stream already exists for aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void StreamAlreadyExists(ILogger logger, Exception exception, string aggregateType, Guid aggregateId);

    [LoggerMessage(EventId = 2613, Level = LogLevel.Error, Message = "Error creating aggregate {AggregateType} with ID {AggregateId}")]
    public static partial void ErrorCreatingAggregate(ILogger logger, Exception exception, string aggregateType, Guid aggregateId);

    // Event Publishing Pipeline Behavior
    [LoggerMessage(EventId = 2614, Level = LogLevel.Debug, Message = "Publishing {EventCount} domain events after command {CommandType}")]
    public static partial void PublishingDomainEvents(ILogger logger, int eventCount, string commandType);

    [LoggerMessage(EventId = 2615, Level = LogLevel.Error, Message = "Failed to publish domain event {EventType}: {ErrorMessage}")]
    public static partial void FailedToPublishDomainEvent(ILogger logger, string eventType, string errorMessage);

    [LoggerMessage(EventId = 2616, Level = LogLevel.Information, Message = "Successfully published {EventCount} domain events after command {CommandType}")]
    public static partial void PublishedDomainEvents(ILogger logger, int eventCount, string commandType);
}
