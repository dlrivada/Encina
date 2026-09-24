namespace Encina.Messaging.DeadLetter;

/// <summary>
/// Data required to create a dead letter message.
/// </summary>
/// <param name="Id">The unique identifier.</param>
/// <param name="RequestType">The request type name.</param>
/// <param name="RequestContent">The request content, serialized through <c>IMessageSerializer</c> (encrypted when message encryption is enabled).</param>
/// <param name="ErrorMessage">
/// The <see cref="EncinaError"/> code of the failure. Never <c>EncinaError.Message</c>, which can carry
/// personal data (#1274).
/// </param>
/// <param name="SourcePattern">The source pattern that produced this dead letter.</param>
/// <param name="TotalRetryAttempts">The total number of retry attempts.</param>
/// <param name="FirstFailedAtUtc">When the message first failed.</param>
/// <param name="DeadLetteredAtUtc">When the message was moved to DLQ.</param>
/// <param name="ExpiresAtUtc">When the message expires.</param>
/// <param name="CorrelationId">The correlation ID.</param>
/// <param name="ExceptionType">The exception type name.</param>
/// <param name="ExceptionMessage">
/// Not populated by <see cref="DeadLetterOrchestrator"/>: an exception message can carry personal data,
/// so only <paramref name="ExceptionType"/> and <paramref name="ExceptionStackTrace"/> are kept.
/// </param>
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
    /// <remarks>
    /// <see cref="DeadLetterOrchestrator"/> builds <paramref name="data"/> for every entry point
    /// (including <see cref="DeadLetterOrchestrator.AddFromFailedMessageAsync"/>), so the request
    /// content has already gone through <c>IMessageSerializer</c> and the error fields hold only
    /// the error code and exception type. Implementations copy the values; they never serialize
    /// the request themselves.
    /// </remarks>
    IDeadLetterMessage Create(DeadLetterData data);
}
