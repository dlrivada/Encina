using Encina.Messaging.Recoverability;

namespace Encina.Messaging.DeadLetter;

/// <summary>
/// Factory for creating dead letter messages.
/// </summary>
/// <remarks>
/// Provider implementations must supply their own factory to create
/// the appropriate concrete message type.
/// </remarks>
public interface IDeadLetterMessageFactory
{
    /// <summary>
    /// Creates a new dead letter message.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="requestType">The request type name.</param>
    /// <param name="requestContent">The serialized request content.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="sourcePattern">The source pattern that produced this dead letter.</param>
    /// <param name="totalRetryAttempts">The total number of retry attempts.</param>
    /// <param name="firstFailedAtUtc">When the message first failed.</param>
    /// <param name="deadLetteredAtUtc">When the message was moved to DLQ.</param>
    /// <param name="expiresAtUtc">When the message expires.</param>
    /// <param name="correlationId">The correlation ID.</param>
    /// <param name="exceptionType">The exception type name.</param>
    /// <param name="exceptionMessage">The exception message.</param>
    /// <param name="exceptionStackTrace">The exception stack trace.</param>
    /// <returns>A new dead letter message.</returns>
    IDeadLetterMessage Create(
        Guid id,
        string requestType,
        string requestContent,
        string errorMessage,
        string sourcePattern,
        int totalRetryAttempts,
        DateTime firstFailedAtUtc,
        DateTime deadLetteredAtUtc,
        DateTime? expiresAtUtc,
        string? correlationId = null,
        string? exceptionType = null,
        string? exceptionMessage = null,
        string? exceptionStackTrace = null);

    /// <summary>
    /// Creates a dead letter message from a failed message record.
    /// </summary>
    /// <param name="failedMessage">The failed message from recoverability pipeline.</param>
    /// <param name="sourcePattern">The source pattern that produced this dead letter.</param>
    /// <param name="expiresAtUtc">When the message expires.</param>
    /// <returns>A new dead letter message.</returns>
    IDeadLetterMessage CreateFromFailedMessage(
        FailedMessage failedMessage,
        string sourcePattern,
        DateTime? expiresAtUtc);
}
