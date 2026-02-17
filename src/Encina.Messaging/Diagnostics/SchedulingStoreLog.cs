using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.Diagnostics;

/// <summary>
/// High-performance logging methods for scheduling store operations using LoggerMessage source generators.
/// </summary>
/// <remarks>
/// <para>
/// Event IDs are allocated in the 2300-2399 range to avoid collisions with other Encina modules.
/// </para>
/// <para>
/// This class uses source generators to create optimized logging methods.
/// Excluded from code coverage as the generated code is boilerplate.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public static partial class SchedulingStoreLog
{
    /// <summary>Logs when scheduling a message for future execution.</summary>
    [LoggerMessage(
        EventId = 2300,
        Level = LogLevel.Debug,
        Message = "Scheduling message {MessageId} ({MessageType}) for {ScheduledAtUtc}")]
    public static partial void SchedulingMessage(
        ILogger logger,
        string messageId,
        string messageType,
        DateTime scheduledAtUtc);

    /// <summary>Logs when a scheduled message has been created.</summary>
    [LoggerMessage(
        EventId = 2301,
        Level = LogLevel.Debug,
        Message = "Scheduled message {MessageId} created")]
    public static partial void MessageCreated(
        ILogger logger,
        string messageId);

    /// <summary>Logs when fetching due scheduled messages.</summary>
    [LoggerMessage(
        EventId = 2302,
        Level = LogLevel.Debug,
        Message = "Fetching due scheduled messages")]
    public static partial void FetchingDueMessages(
        ILogger logger);

    /// <summary>Logs when due scheduled messages have been fetched.</summary>
    [LoggerMessage(
        EventId = 2303,
        Level = LogLevel.Debug,
        Message = "Fetched {Count} due scheduled messages")]
    public static partial void FetchedDueMessages(
        ILogger logger,
        int count);

    /// <summary>Logs when marking a scheduled message as executed.</summary>
    [LoggerMessage(
        EventId = 2304,
        Level = LogLevel.Debug,
        Message = "Marking scheduled message {MessageId} as executed")]
    public static partial void MarkingMessageAsExecuted(
        ILogger logger,
        string messageId);

    /// <summary>Logs when a scheduling store operation fails with a domain error.</summary>
    [LoggerMessage(
        EventId = 2305,
        Level = LogLevel.Warning,
        Message = "Scheduling store operation failed: {ErrorMessage}")]
    public static partial void OperationFailed(
        ILogger logger,
        string errorMessage);

    /// <summary>Logs when a scheduling store operation throws an unexpected exception.</summary>
    [LoggerMessage(
        EventId = 2306,
        Level = LogLevel.Error,
        Message = "Scheduling store operation threw an unexpected exception")]
    public static partial void OperationException(
        ILogger logger,
        Exception exception);
}
