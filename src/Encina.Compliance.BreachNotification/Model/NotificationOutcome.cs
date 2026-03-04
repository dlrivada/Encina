namespace Encina.Compliance.BreachNotification.Model;

/// <summary>
/// Outcome of a breach notification attempt to a supervisory authority or data subjects.
/// </summary>
/// <remarks>
/// <para>
/// Tracks the result of notification attempts required under GDPR Articles 33 and 34.
/// Each notification attempt (authority or data subject) produces a <see cref="NotificationResult"/>
/// containing an outcome value indicating whether the notification was successfully
/// delivered, is still pending, failed, or was exempted.
/// </para>
/// <para>
/// Per GDPR Article 33(5), the controller must document the facts relating to the personal
/// data breach, its effects and the remedial action taken, including notification outcomes.
/// </para>
/// </remarks>
public enum NotificationOutcome
{
    /// <summary>
    /// The notification was successfully sent to the recipient.
    /// </summary>
    Sent = 0,

    /// <summary>
    /// The notification attempt failed due to a delivery error.
    /// </summary>
    /// <remarks>
    /// The <see cref="NotificationResult.ErrorMessage"/> property provides
    /// additional details about the failure reason.
    /// </remarks>
    Failed = 1,

    /// <summary>
    /// The notification is queued and awaiting delivery.
    /// </summary>
    Pending = 2,

    /// <summary>
    /// The notification was not sent because an exemption applies.
    /// </summary>
    /// <remarks>
    /// Per Art. 34(3), data subject notification is not required when an exemption
    /// applies (e.g., encryption protection, mitigating measures, or disproportionate effort).
    /// The specific exemption is recorded in the <see cref="SubjectNotificationExemption"/>
    /// field of the <see cref="BreachRecord"/>.
    /// </remarks>
    Exempted = 3
}
