using Microsoft.Extensions.Logging;

namespace Encina.NATS;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Publishing message of type {MessageType} to subject {Subject}")]
    public static partial void PublishingMessage(ILogger logger, string messageType, string subject);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Successfully published message to subject {Subject}")]
    public static partial void SuccessfullyPublishedMessage(ILogger logger, string subject);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to publish message of type {MessageType} to subject {Subject}")]
    public static partial void FailedToPublishMessage(ILogger logger, Exception exception, string messageType, string subject);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Sending request of type {RequestType} to subject {Subject}")]
    public static partial void SendingRequest(ILogger logger, string requestType, string subject);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Successfully received response for request of type {RequestType}")]
    public static partial void SuccessfullyReceivedResponse(ILogger logger, string requestType);

    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Failed to send request of type {RequestType}")]
    public static partial void FailedToSendRequest(ILogger logger, Exception exception, string requestType);

    [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = "Publishing message of type {MessageType} to JetStream subject {Subject}")]
    public static partial void PublishingToJetStream(ILogger logger, string messageType, string subject);

    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "Successfully published message to stream {Stream} with sequence {Sequence}")]
    public static partial void SuccessfullyPublishedToJetStream(ILogger logger, string stream, ulong sequence);

    [LoggerMessage(EventId = 9, Level = LogLevel.Error, Message = "Failed to publish message of type {MessageType} to JetStream")]
    public static partial void FailedToPublishToJetStream(ILogger logger, Exception exception, string messageType);
}
