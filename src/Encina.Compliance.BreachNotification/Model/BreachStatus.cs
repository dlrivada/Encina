namespace Encina.Compliance.BreachNotification.Model;

/// <summary>
/// Lifecycle status of a data breach record.
/// </summary>
/// <remarks>
/// <para>
/// Each breach record progresses through a defined lifecycle reflecting the notification
/// obligations under GDPR Articles 33 and 34. The status tracks whether the required
/// notifications have been sent within the 72-hour deadline (Art. 33(1)).
/// </para>
/// <para>
/// Typical lifecycle: <c>Detected</c> → <c>Investigating</c> → <c>AuthorityNotified</c>
/// → <c>SubjectsNotified</c> → <c>Resolved</c> → <c>Closed</c>.
/// Not all breaches require data subject notification — only those that are "likely to
/// result in a high risk" per Art. 34(1).
/// </para>
/// </remarks>
public enum BreachStatus
{
    /// <summary>
    /// The breach has been detected and the 72-hour notification countdown has started.
    /// </summary>
    /// <remarks>
    /// Per Art. 33(1), the controller has 72 hours from "becoming aware" of the breach
    /// to notify the supervisory authority. Detection is the point of awareness.
    /// </remarks>
    Detected = 0,

    /// <summary>
    /// The breach is under active investigation to determine scope and severity.
    /// </summary>
    /// <remarks>
    /// Per Art. 33(4), if the full scope is not yet known, the controller may
    /// submit an initial notification and provide additional information in phases.
    /// Investigation should not delay the 72-hour notification deadline.
    /// </remarks>
    Investigating = 1,

    /// <summary>
    /// The supervisory authority has been notified of the breach.
    /// </summary>
    /// <remarks>
    /// Per Art. 33(3), the notification must include: the nature of the breach,
    /// approximate number of subjects, data categories, DPO contact, likely
    /// consequences, and measures taken or proposed.
    /// </remarks>
    AuthorityNotified = 2,

    /// <summary>
    /// Affected data subjects have been notified of the breach.
    /// </summary>
    /// <remarks>
    /// Per Art. 34(1), this is required when the breach "is likely to result in a high risk
    /// to the rights and freedoms of natural persons." Communication must be in clear
    /// and plain language (Art. 34(2)).
    /// </remarks>
    SubjectsNotified = 3,

    /// <summary>
    /// The breach has been resolved and remediation measures are in place.
    /// </summary>
    /// <remarks>
    /// The root cause has been identified and addressed. All required notifications
    /// have been sent. Ongoing monitoring may continue to verify effectiveness
    /// of remediation measures.
    /// </remarks>
    Resolved = 4,

    /// <summary>
    /// The breach case is closed. All obligations have been fulfilled.
    /// </summary>
    /// <remarks>
    /// Per Art. 33(5), the controller must document all breaches including their effects
    /// and remedial actions taken. The closed record serves as part of this documentation.
    /// </remarks>
    Closed = 5
}
