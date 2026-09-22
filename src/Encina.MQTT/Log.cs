using Microsoft.Extensions.Logging;

namespace Encina.MQTT;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 4250, Level = LogLevel.Debug, Message = "Publishing message of type {MessageType} to topic {Topic} with QoS {QoS}")]
    public static partial void PublishingMessage(ILogger logger, string messageType, string topic, MqttQualityOfService qoS);

    [LoggerMessage(EventId = 4251, Level = LogLevel.Debug, Message = "Successfully published message to topic {Topic}")]
    public static partial void SuccessfullyPublishedMessage(ILogger logger, string topic);

    [LoggerMessage(EventId = 4252, Level = LogLevel.Error, Message = "Failed to publish message of type {MessageType} to topic {Topic}")]
    public static partial void FailedToPublishMessage(ILogger logger, Exception exception, string messageType, string topic);

    [LoggerMessage(EventId = 4253, Level = LogLevel.Debug, Message = "Subscribing to topic {Topic} for messages of type {MessageType}")]
    public static partial void SubscribingToTopic(ILogger logger, string topic, string messageType);

    [LoggerMessage(EventId = 4254, Level = LogLevel.Debug, Message = "Subscribing to topic pattern {TopicFilter} for messages of type {MessageType}")]
    public static partial void SubscribingToPattern(ILogger logger, string topicFilter, string messageType);

    [LoggerMessage(EventId = 4255, Level = LogLevel.Error, Message = "Error processing MQTT message from topic {Topic}")]
    public static partial void ErrorProcessingMessage(ILogger logger, Exception exception, string topic);
}
