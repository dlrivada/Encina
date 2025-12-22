using Microsoft.Extensions.Logging;

namespace Encina.Kafka;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Producing message of type {MessageType} to topic {Topic}")]
    public static partial void ProducingMessage(ILogger logger, string messageType, string topic);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Successfully produced message to topic {Topic} partition {Partition} offset {Offset}")]
    public static partial void SuccessfullyProducedMessage(ILogger logger, string topic, int partition, long offset);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to produce message of type {MessageType} to topic {Topic}")]
    public static partial void FailedToProduceMessage(ILogger logger, Exception exception, string messageType, string topic);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Failed to produce batch of messages of type {MessageType}")]
    public static partial void FailedToProduceBatch(ILogger logger, Exception exception, string messageType);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Producing message of type {MessageType} with {HeaderCount} headers to topic {Topic}")]
    public static partial void ProducingMessageWithHeaders(ILogger logger, string messageType, int headerCount, string topic);

    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Failed to produce message of type {MessageType} with headers")]
    public static partial void FailedToProduceMessageWithHeaders(ILogger logger, Exception exception, string messageType);
}
