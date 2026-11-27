using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.Diagnostics;

/// <summary>
/// High-performance logging methods for outbox store operations using LoggerMessage source generators.
/// </summary>
/// <remarks>
/// <para>
/// Event IDs are allocated in the 10800-10899 range to avoid collisions with other Encina modules.
/// </para>
/// <para>
/// This class uses source generators to create optimized logging methods.
/// Excluded from code coverage as the generated code is boilerplate.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public static partial class OutboxStoreLog
{
    /// <summary>Logs when a message is being added to the outbox.</summary>
    [LoggerMessage(
        EventId = 10800,
        Level = LogLevel.Debug,
        Message = "Adding message {MessageId} to outbox ({MessageType})")]
    public static partial void AddingMessage(
        ILogger logger,
        string messageId,
        string messageType);

    /// <summary>Logs when a message has been successfully added to the outbox.</summary>
    [LoggerMessage(
        EventId = 10801,
        Level = LogLevel.Debug,
        Message = "Outbox message {MessageId} added")]
    public static partial void MessageAdded(
        ILogger logger,
        string messageId);

    /// <summary>Logs when fetching pending outbox messages.</summary>
    [LoggerMessage(
        EventId = 10802,
        Level = LogLevel.Debug,
        Message = "Fetching pending outbox messages (BatchSize: {BatchSize})")]
    public static partial void FetchingPendingMessages(
        ILogger logger,
        int batchSize);

    /// <summary>Logs when pending outbox messages have been fetched.</summary>
    [LoggerMessage(
        EventId = 10803,
        Level = LogLevel.Debug,
        Message = "Fetched {Count} pending outbox messages")]
    public static partial void FetchedPendingMessages(
        ILogger logger,
        int count);

    /// <summary>Logs when marking an outbox message as processed.</summary>
    [LoggerMessage(
        EventId = 10804,
        Level = LogLevel.Debug,
        Message = "Marking outbox message {MessageId} as processed")]
    public static partial void MarkingMessageAsProcessed(
        ILogger logger,
        string messageId);

    /// <summary>Logs when an outbox store operation fails with a domain error.</summary>
    [LoggerMessage(
        EventId = 10805,
        Level = LogLevel.Warning,
        Message = "Outbox store operation failed: {ErrorMessage}")]
    public static partial void OperationFailed(
        ILogger logger,
        string errorMessage);

    /// <summary>Logs when an outbox store operation throws an unexpected exception.</summary>
    [LoggerMessage(
        EventId = 10806,
        Level = LogLevel.Error,
        Message = "Outbox store operation threw an unexpected exception")]
    public static partial void OperationException(
        ILogger logger,
        Exception exception);
}
