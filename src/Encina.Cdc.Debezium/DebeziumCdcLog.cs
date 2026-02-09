using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Cdc.Debezium;

/// <summary>
/// High-performance logging methods using LoggerMessage source generators
/// for the Debezium CDC provider.
/// </summary>
[ExcludeFromCodeCoverage]
internal static partial class DebeziumCdcLog
{
    /// <summary>Logs when the HTTP listener starts.</summary>
    [LoggerMessage(
        EventId = 300,
        Level = LogLevel.Information,
        Message = "Debezium HTTP listener started on {Prefix}")]
    public static partial void ListenerStarted(ILogger logger, string prefix);

    /// <summary>Logs when the HTTP listener stops.</summary>
    [LoggerMessage(
        EventId = 301,
        Level = LogLevel.Information,
        Message = "Debezium HTTP listener stopped")]
    public static partial void ListenerStopped(ILogger logger);

    /// <summary>Logs when a Debezium event is received.</summary>
    [LoggerMessage(
        EventId = 302,
        Level = LogLevel.Debug,
        Message = "Debezium event received and queued")]
    public static partial void EventReceived(ILogger logger);

    /// <summary>Logs when an HTTP request fails.</summary>
    [LoggerMessage(
        EventId = 303,
        Level = LogLevel.Error,
        Message = "Failed to process Debezium HTTP request")]
    public static partial void RequestFailed(ILogger logger, Exception exception);

    /// <summary>Logs when the connector resumes from a saved position.</summary>
    [LoggerMessage(
        EventId = 304,
        Level = LogLevel.Information,
        Message = "Debezium connector resuming from position: {Position}")]
    public static partial void ResumingFromPosition(ILogger logger, string position);

    /// <summary>Logs when the internal channel is full and backpressure is applied.</summary>
    [LoggerMessage(
        EventId = 305,
        Level = LogLevel.Warning,
        Message = "Debezium channel full ({Capacity} items), returning 503 backpressure")]
    public static partial void ChannelFull(ILogger logger, int capacity);

    /// <summary>Logs when the HTTP listener is retrying after a start failure.</summary>
    [LoggerMessage(
        EventId = 306,
        Level = LogLevel.Warning,
        Message = "Debezium HTTP listener start failed, retrying {Attempt}/{MaxRetries} after {Delay}")]
    public static partial void ListenerRetrying(ILogger logger, int attempt, int maxRetries, TimeSpan delay);

    /// <summary>Logs when the HTTP listener fails to start after all retries.</summary>
    [LoggerMessage(
        EventId = 307,
        Level = LogLevel.Error,
        Message = "Debezium HTTP listener failed to start after {Attempts} attempts")]
    public static partial void ListenerStartFailed(ILogger logger, Exception exception, int attempts);

    /// <summary>Logs when an event is skipped because it was already processed.</summary>
    [LoggerMessage(
        EventId = 308,
        Level = LogLevel.Debug,
        Message = "Debezium event skipped (already processed, position <= resume point)")]
    public static partial void EventSkippedAlreadyProcessed(ILogger logger);

    /// <summary>Logs when an event is missing the 'op' field (schema change event).</summary>
    [LoggerMessage(
        EventId = 309,
        Level = LogLevel.Warning,
        Message = "Debezium event missing 'op' field, skipping as schema change event")]
    public static partial void SchemaChangeEventSkipped(ILogger logger);

    /// <summary>Logs when position retrieval fails during resume.</summary>
    [LoggerMessage(
        EventId = 310,
        Level = LogLevel.Warning,
        Message = "Failed to retrieve saved position for connector '{ConnectorId}', starting from beginning")]
    public static partial void PositionRetrievalFailed(ILogger logger, Exception exception, string connectorId);
}
