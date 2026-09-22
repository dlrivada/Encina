using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Cdc.Debezium.Kafka;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators
/// for the Debezium Kafka CDC provider.
/// </summary>
[ExcludeFromCodeCoverage]
internal static partial class DebeziumKafkaLog
{
    /// <summary>Logs when the Kafka consumer starts and subscribes to topics.</summary>
    [LoggerMessage(
        EventId = 4961,
        Level = LogLevel.Information,
        Message = "Debezium Kafka consumer started, subscribed to topics: {Topics}")]
    public static partial void ConsumerStarted(ILogger logger, string topics);

    /// <summary>Logs when the Kafka consumer stops.</summary>
    [LoggerMessage(
        EventId = 4962,
        Level = LogLevel.Information,
        Message = "Debezium Kafka consumer stopped")]
    public static partial void ConsumerStopped(ILogger logger);

    /// <summary>Logs when a Kafka event is consumed.</summary>
    [LoggerMessage(
        EventId = 4963,
        Level = LogLevel.Debug,
        Message = "Debezium Kafka event consumed from {Topic}[{Partition}]@{Offset}")]
    public static partial void EventConsumed(ILogger logger, string topic, int partition, long offset);

    /// <summary>Logs when Kafka partitions are assigned during a rebalance.</summary>
    [LoggerMessage(
        EventId = 4964,
        Level = LogLevel.Warning,
        Message = "Debezium Kafka consumer rebalance: partitions assigned: {Partitions}")]
    public static partial void PartitionsAssigned(ILogger logger, string partitions);

    /// <summary>Logs when Kafka partitions are revoked during a rebalance.</summary>
    [LoggerMessage(
        EventId = 4965,
        Level = LogLevel.Warning,
        Message = "Debezium Kafka consumer rebalance: partitions revoked: {Partitions}")]
    public static partial void PartitionsRevoked(ILogger logger, string partitions);

    /// <summary>Logs when the Kafka consumer encounters an error.</summary>
    [LoggerMessage(
        EventId = 4966,
        Level = LogLevel.Error,
        Message = "Debezium Kafka consumer error: {ErrorReason}")]
    public static partial void ConsumerError(ILogger logger, string errorReason);

    /// <summary>Logs when the Kafka connector resumes from a saved offset.</summary>
    [LoggerMessage(
        EventId = 4967,
        Level = LogLevel.Information,
        Message = "Debezium Kafka connector resuming from offset {Offset} on {Topic}[{Partition}]")]
    public static partial void ResumingFromOffset(ILogger logger, long offset, string topic, int partition);

    /// <summary>Logs when a Kafka event is skipped because it was already processed.</summary>
    [LoggerMessage(
        EventId = 4968,
        Level = LogLevel.Debug,
        Message = "Debezium Kafka event skipped (already processed, offset <= resume point)")]
    public static partial void EventSkippedAlreadyProcessed(ILogger logger);
}
