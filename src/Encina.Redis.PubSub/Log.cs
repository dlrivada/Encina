using Microsoft.Extensions.Logging;

namespace Encina.Redis.PubSub;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Publishing message of type {MessageType} to channel {Channel}")]
    public static partial void PublishingMessage(ILogger logger, string messageType, string channel);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Successfully published message to {SubscriberCount} subscribers")]
    public static partial void SuccessfullyPublishedMessage(ILogger logger, long subscriberCount);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to publish message of type {MessageType} to channel {Channel}")]
    public static partial void FailedToPublishMessage(ILogger logger, Exception exception, string messageType, string channel);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Subscribing to channel {Channel} for messages of type {MessageType}")]
    public static partial void SubscribingToChannel(ILogger logger, string channel, string messageType);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Subscribing to pattern {Pattern} for messages of type {MessageType}")]
    public static partial void SubscribingToPattern(ILogger logger, string pattern, string messageType);

    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Error processing message from channel {Channel}")]
    public static partial void ErrorProcessingMessage(ILogger logger, Exception exception, string channel);
}
