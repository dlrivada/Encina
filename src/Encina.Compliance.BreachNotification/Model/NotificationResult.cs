namespace Encina.Compliance.BreachNotification.Model;

/// <summary>
/// Captures the result of a breach notification attempt to a supervisory authority
/// or data subjects.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 33(5), the controller must document all breach notification
/// attempts, including their outcomes. Each call to <c>IBreachNotifier.NotifyAuthorityAsync</c>
/// or <c>IBreachNotifier.NotifyDataSubjectsAsync</c> produces a <see cref="NotificationResult"/>
/// that is recorded in the audit trail.
/// </para>
/// <para>
/// A failed notification does not relieve the controller of the obligation to notify.
/// Failed attempts should be retried, and the delay reason should be documented
/// per Art. 33(1).
/// </para>
/// </remarks>
public sealed record NotificationResult
{
    /// <summary>
    /// Outcome of the notification attempt.
    /// </summary>
    public required NotificationOutcome Outcome { get; init; }

    /// <summary>
    /// Timestamp when the notification was sent (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> when the notification has not been sent (e.g., <see cref="NotificationOutcome.Pending"/>
    /// or <see cref="NotificationOutcome.Failed"/>).
    /// </remarks>
    public DateTimeOffset? SentAtUtc { get; init; }

    /// <summary>
    /// Identifier or description of the notification recipient.
    /// </summary>
    /// <remarks>
    /// For authority notifications, this is the supervisory authority name or identifier.
    /// For data subject notifications, this may be a group identifier or count description.
    /// <c>null</c> when the recipient is not yet determined.
    /// </remarks>
    public string? Recipient { get; init; }

    /// <summary>
    /// Error message describing why the notification failed.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when the notification was successful or is still pending.
    /// </remarks>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Identifier of the breach this notification result belongs to.
    /// </summary>
    public required string BreachId { get; init; }
}
