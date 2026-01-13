using Encina.Messaging.Recoverability;

namespace Encina.Messaging.DeadLetter;

/// <summary>
/// Data required to create a dead letter message.
/// </summary>
/// <param name="Id">The unique identifier.</param>
/// <param name="RequestType">The request type name.</param>
/// <param name="RequestContent">The serialized request content.</param>
/// <param name="ErrorMessage">The error message.</param>
/// <param name="SourcePattern">The source pattern that produced this dead letter.</param>
/// <param name="TotalRetryAttempts">The total number of retry attempts.</param>
/// <param name="FirstFailedAtUtc">When the message first failed.</param>
/// <param name="DeadLetteredAtUtc">When the message was moved to DLQ.</param>
/// <param name="ExpiresAtUtc">When the message expires.</param>
/// <param name="CorrelationId">The correlation ID.</param>
/// <param name="ExceptionType">The exception type name.</param>
/// <param name="ExceptionMessage">The exception message.</param>
/// <param name="ExceptionStackTrace">The exception stack trace.</param>
public sealed record DeadLetterData(
    Guid Id,
    string RequestType,
    string RequestContent,
    string ErrorMessage,
    string SourcePattern,
    int TotalRetryAttempts,
    DateTime FirstFailedAtUtc,
    DateTime DeadLetteredAtUtc,
    DateTime? ExpiresAtUtc,
    string? CorrelationId = null,
    string? ExceptionType = null,
    string? ExceptionMessage = null,
    string? ExceptionStackTrace = null);

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
    /// <param name="data">The data for creating the dead letter message.</param>
    /// <returns>A new dead letter message.</returns>
    IDeadLetterMessage Create(DeadLetterData data);

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
