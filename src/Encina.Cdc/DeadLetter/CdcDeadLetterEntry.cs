namespace Encina.Cdc.DeadLetter;

/// <summary>
/// Represents a CDC change event that has been moved to the dead letter queue
/// after exhausting all retry attempts.
/// </summary>
/// <param name="Id">Unique identifier for this dead letter entry.</param>
/// <param name="OriginalEvent">The original <see cref="ChangeEvent"/> that failed processing.</param>
/// <param name="ErrorMessage">The error message from the last failed processing attempt.</param>
/// <param name="StackTrace">The stack trace from the last failed processing attempt.</param>
/// <param name="RetryCount">The number of retry attempts that were made before dead-lettering.</param>
/// <param name="FailedAtUtc">The UTC timestamp when the event was moved to the dead letter queue.</param>
/// <param name="ConnectorId">The identifier of the CDC connector that produced this event.</param>
/// <param name="Status">The current resolution status of this dead letter entry.</param>
public sealed record CdcDeadLetterEntry(
    Guid Id,
    ChangeEvent OriginalEvent,
    string ErrorMessage,
    string StackTrace,
    int RetryCount,
    DateTime FailedAtUtc,
    string ConnectorId,
    CdcDeadLetterStatus Status);
