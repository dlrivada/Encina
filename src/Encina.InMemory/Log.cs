using Microsoft.Extensions.Logging;

namespace Encina.InMemory;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "In-memory message bus started with {WorkerCount} workers")]
    public static partial void MessageBusStarted(ILogger logger, int workerCount);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Publishing message of type {MessageType}")]
    public static partial void PublishingMessage(ILogger logger, string messageType);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Successfully published message of type {MessageType}")]
    public static partial void SuccessfullyPublishedMessage(ILogger logger, string messageType);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Failed to publish message of type {MessageType}")]
    public static partial void FailedToPublishMessage(ILogger logger, Exception exception, string messageType);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Enqueueing message of type {MessageType}")]
    public static partial void EnqueuingMessage(ILogger logger, string messageType);

    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "Successfully enqueued message of type {MessageType}")]
    public static partial void SuccessfullyEnqueuedMessage(ILogger logger, string messageType);

    [LoggerMessage(EventId = 7, Level = LogLevel.Error, Message = "Failed to enqueue message of type {MessageType}")]
    public static partial void FailedToEnqueueMessage(ILogger logger, Exception exception, string messageType);

    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "Subscribed to messages of type {MessageType}")]
    public static partial void SubscribedToMessages(ILogger logger, string messageType);

    [LoggerMessage(EventId = 9, Level = LogLevel.Error, Message = "Error processing message of type {MessageType}")]
    public static partial void ErrorProcessingMessage(ILogger logger, Exception exception, string messageType);

    [LoggerMessage(EventId = 10, Level = LogLevel.Error, Message = "Error processing queued message")]
    public static partial void ErrorProcessingQueuedMessage(ILogger logger, Exception exception);
}
