namespace Encina.Messaging.DeadLetter;

/// <summary>
/// Represents a message that has permanently failed and is stored in the Dead Letter Queue.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides a provider-agnostic abstraction for dead letter messages.
/// Implementations can use Entity Framework Core, Dapper, ADO.NET, or any custom storage.
/// </para>
/// <para>
/// Dead letter messages contain all the information needed to:
/// <list type="bullet">
/// <item><description>Understand why the message failed</description></item>
/// <item><description>Replay the message when the issue is resolved</description></item>
/// <item><description>Monitor and alert on DLQ accumulation</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IDeadLetterMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for the dead letter message.
    /// </summary>
    Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified type name of the original request.
    /// </summary>
    string RequestType { get; set; }

    /// <summary>
    /// Gets or sets the serialized request content.
    /// </summary>
    string RequestContent { get; set; }

    /// <summary>
    /// Gets or sets the error message describing the failure.
    /// </summary>
    string ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the exception type if an exception was thrown, otherwise null.
    /// </summary>
    string? ExceptionType { get; set; }

    /// <summary>
    /// Gets or sets the exception message if an exception was thrown, otherwise null.
    /// </summary>
    string? ExceptionMessage { get; set; }

    /// <summary>
    /// Gets or sets the exception stack trace if an exception was thrown, otherwise null.
    /// </summary>
    string? ExceptionStackTrace { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID from the original request context.
    /// </summary>
    string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the source pattern that produced this dead letter.
    /// </summary>
    /// <remarks>
    /// Examples: "Outbox", "Inbox", "Recoverability", "Saga", "Scheduling".
    /// </remarks>
    string SourcePattern { get; set; }

    /// <summary>
    /// Gets or sets the total number of retry attempts made before dead lettering.
    /// </summary>
    int TotalRetryAttempts { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the message first failed.
    /// </summary>
    DateTime FirstFailedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the message was moved to DLQ.
    /// </summary>
    DateTime DeadLetteredAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the message expires and can be cleaned up.
    /// </summary>
    DateTime? ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the message was replayed, if applicable.
    /// </summary>
    DateTime? ReplayedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the result of the replay attempt, if applicable.
    /// </summary>
    string? ReplayResult { get; set; }

    /// <summary>
    /// Gets a value indicating whether this message has been replayed.
    /// </summary>
    bool IsReplayed { get; }

    /// <summary>
    /// Gets a value indicating whether this message has expired.
    /// </summary>
    bool IsExpired { get; }
}
