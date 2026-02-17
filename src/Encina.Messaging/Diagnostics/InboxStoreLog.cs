using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.Diagnostics;

/// <summary>
/// High-performance logging methods for inbox store operations using LoggerMessage source generators.
/// </summary>
/// <remarks>
/// <para>
/// Event IDs are allocated in the 2100-2199 range to avoid collisions with other Encina modules.
/// </para>
/// <para>
/// This class uses source generators to create optimized logging methods.
/// Excluded from code coverage as the generated code is boilerplate.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public static partial class InboxStoreLog
{
    /// <summary>Logs when checking the inbox for a duplicate message.</summary>
    [LoggerMessage(
        EventId = 2100,
        Level = LogLevel.Debug,
        Message = "Checking inbox for duplicate message {MessageId}")]
    public static partial void CheckingForDuplicate(
        ILogger logger,
        string messageId);

    /// <summary>Logs when a duplicate message is detected in the inbox.</summary>
    [LoggerMessage(
        EventId = 2101,
        Level = LogLevel.Debug,
        Message = "Duplicate message {MessageId} detected in inbox")]
    public static partial void DuplicateDetected(
        ILogger logger,
        string messageId);

    /// <summary>Logs when a message is being added to the inbox.</summary>
    [LoggerMessage(
        EventId = 2102,
        Level = LogLevel.Debug,
        Message = "Adding message {MessageId} to inbox ({MessageType})")]
    public static partial void AddingMessage(
        ILogger logger,
        string messageId,
        string messageType);

    /// <summary>Logs when a message has been successfully added to the inbox.</summary>
    [LoggerMessage(
        EventId = 2103,
        Level = LogLevel.Debug,
        Message = "Inbox message {MessageId} added")]
    public static partial void MessageAdded(
        ILogger logger,
        string messageId);

    /// <summary>Logs when marking an inbox message as processed.</summary>
    [LoggerMessage(
        EventId = 2104,
        Level = LogLevel.Debug,
        Message = "Marking inbox message {MessageId} as processed")]
    public static partial void MarkingMessageAsProcessed(
        ILogger logger,
        string messageId);

    /// <summary>Logs when an inbox store operation fails with a domain error.</summary>
    [LoggerMessage(
        EventId = 2105,
        Level = LogLevel.Warning,
        Message = "Inbox store operation failed: {ErrorMessage}")]
    public static partial void OperationFailed(
        ILogger logger,
        string errorMessage);

    /// <summary>Logs when an inbox store operation throws an unexpected exception.</summary>
    [LoggerMessage(
        EventId = 2106,
        Level = LogLevel.Error,
        Message = "Inbox store operation threw an unexpected exception")]
    public static partial void OperationException(
        ILogger logger,
        Exception exception);
}
