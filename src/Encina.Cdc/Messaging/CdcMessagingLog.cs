using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Cdc.Messaging;

/// <summary>
/// High-performance structured logging for CDC messaging integration.
/// Uses <see cref="LoggerMessageAttribute"/> source generators.
/// </summary>
[ExcludeFromCodeCoverage]
internal static partial class CdcMessagingLog
{
    [LoggerMessage(
        EventId = 130,
        Level = LogLevel.Debug,
        Message = "Publishing CDC change notification for table '{TableName}' operation {Operation} with topic '{TopicName}'")]
    public static partial void PublishingChangeNotification(
        ILogger logger, string tableName, ChangeOperation operation, string topicName);

    [LoggerMessage(
        EventId = 131,
        Level = LogLevel.Debug,
        Message = "Published CDC change notification for table '{TableName}' operation {Operation}")]
    public static partial void PublishedChangeNotification(
        ILogger logger, string tableName, ChangeOperation operation);

    [LoggerMessage(
        EventId = 132,
        Level = LogLevel.Debug,
        Message = "CDC change event filtered out for table '{TableName}' operation {Operation}")]
    public static partial void ChangeEventFiltered(
        ILogger logger, string tableName, ChangeOperation operation);

    [LoggerMessage(
        EventId = 133,
        Level = LogLevel.Debug,
        Message = "Processing outbox CDC event for message type '{NotificationType}'")]
    public static partial void OutboxCdcProcessing(
        ILogger logger, string notificationType);

    [LoggerMessage(
        EventId = 134,
        Level = LogLevel.Debug,
        Message = "Published outbox CDC notification of type '{NotificationType}'")]
    public static partial void OutboxCdcPublished(
        ILogger logger, string notificationType);

    [LoggerMessage(
        EventId = 135,
        Level = LogLevel.Debug,
        Message = "Skipping outbox CDC event - already processed (ProcessedAtUtc is set)")]
    public static partial void OutboxCdcSkippedAlreadyProcessed(ILogger logger);

    [LoggerMessage(
        EventId = 136,
        Level = LogLevel.Warning,
        Message = "Failed to deserialize outbox CDC notification of type '{NotificationType}'")]
    public static partial void OutboxCdcDeserializationFailed(
        ILogger logger, string notificationType);
}
