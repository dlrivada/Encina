namespace Encina.Compliance.BreachNotification.Model;

/// <summary>
/// Represents an entry in the breach notification audit trail for demonstrating compliance.
/// </summary>
/// <remarks>
/// <para>
/// Each audit entry records a specific action taken during the breach notification
/// lifecycle: breach detection, authority notification, data subject notification,
/// phased report submission, deadline warning, and breach resolution.
/// </para>
/// <para>
/// Per GDPR Article 33(5), the controller must document the facts relating to the
/// personal data breach, its effects and the remedial action taken. This documentation
/// must be sufficient to enable the supervisory authority to verify compliance with
/// Articles 33 and 34.
/// </para>
/// <para>
/// Per GDPR Article 5(2) (accountability principle), controllers must demonstrate
/// compliance with data protection principles. Breach audit entries provide a
/// complete, immutable record of all breach-related actions and decisions.
/// </para>
/// <para>
/// Audit entries should never be modified or deleted. They serve as legal evidence
/// of the notification measures applied and may be required during regulatory audits
/// or supervisory authority inquiries (Article 58).
/// </para>
/// </remarks>
public sealed record BreachAuditEntry
{
    /// <summary>
    /// Unique identifier for this audit entry.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Identifier of the breach this audit entry relates to.
    /// </summary>
    public required string BreachId { get; init; }

    /// <summary>
    /// The action that was performed.
    /// </summary>
    /// <example>
    /// "BreachDetected", "AuthorityNotified", "SubjectsNotified", "PhasedReportSubmitted",
    /// "DeadlineWarning", "BreachResolved", "StatusChanged", "ExemptionApplied"
    /// </example>
    public required string Action { get; init; }

    /// <summary>
    /// Additional details about the action performed.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when no additional context is needed. For notification actions,
    /// this may contain the notification outcome. For deadline warnings, this may
    /// contain the remaining hours.
    /// </remarks>
    public string? Detail { get; init; }

    /// <summary>
    /// Identifier of the user or system that performed the action.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for automated system actions (e.g., deadline monitoring,
    /// automatic breach detection by the pipeline behavior).
    /// </remarks>
    public string? PerformedByUserId { get; init; }

    /// <summary>
    /// Timestamp when the action occurred (UTC).
    /// </summary>
    public required DateTimeOffset OccurredAtUtc { get; init; }

    /// <summary>
    /// Creates a new breach audit entry with a generated unique identifier
    /// and the current UTC timestamp.
    /// </summary>
    /// <param name="breachId">Identifier of the related breach.</param>
    /// <param name="action">The action that was performed.</param>
    /// <param name="detail">Additional details about the action.</param>
    /// <param name="performedByUserId">Identifier of the actor who performed the action.</param>
    /// <returns>A new <see cref="BreachAuditEntry"/> with a generated GUID identifier.</returns>
    public static BreachAuditEntry Create(
        string breachId,
        string action,
        string? detail = null,
        string? performedByUserId = null) =>
        new()
        {
            Id = Guid.NewGuid().ToString("N"),
            BreachId = breachId,
            Action = action,
            Detail = detail,
            PerformedByUserId = performedByUserId,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
}
