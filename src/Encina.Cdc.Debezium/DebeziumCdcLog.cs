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
}
