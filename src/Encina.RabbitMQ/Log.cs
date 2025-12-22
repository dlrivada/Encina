using Microsoft.Extensions.Logging;

namespace Encina.RabbitMQ;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Publishing message of type {MessageType} to exchange {Exchange} with routing key {RoutingKey}")]
    public static partial void PublishingMessage(ILogger logger, string messageType, string exchange, string routingKey);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Successfully published message of type {MessageType}")]
    public static partial void SuccessfullyPublishedMessage(ILogger logger, string messageType);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to publish message of type {MessageType}")]
    public static partial void FailedToPublishMessage(ILogger logger, Exception exception, string messageType);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Sending message of type {MessageType} to queue {Queue}")]
    public static partial void SendingToQueue(ILogger logger, string messageType, string queue);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Successfully sent message of type {MessageType} to queue {Queue}")]
    public static partial void SuccessfullySentToQueue(ILogger logger, string messageType, string queue);

    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Failed to send message of type {MessageType} to queue {Queue}")]
    public static partial void FailedToSendToQueue(ILogger logger, Exception exception, string messageType, string queue);
}
