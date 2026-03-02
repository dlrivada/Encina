namespace Encina.Compliance.Retention.Model;

/// <summary>
/// Represents an entry in the retention audit trail for demonstrating compliance.
/// </summary>
/// <remarks>
/// <para>
/// Each audit entry records a specific action taken by the retention system:
/// policy creation, record tracking, enforcement execution, legal hold application
/// or release, and data deletion. The audit trail is immutable and provides evidence
/// of compliance with GDPR obligations regarding storage limitation.
/// </para>
/// <para>
/// Per GDPR Article 5(2) (accountability principle), controllers must demonstrate
/// compliance with data protection principles. Retention audit entries provide a
/// complete, immutable record of all retention-related actions and decisions.
/// </para>
/// <para>
/// Audit entries should never be modified or deleted. They serve as legal evidence
/// of the retention and deletion measures applied and may be required during
/// regulatory audits or supervisory authority inquiries (Article 58).
/// </para>
/// </remarks>
public sealed record RetentionAuditEntry
{
    /// <summary>
    /// Unique identifier for this audit entry.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The action that was performed.
    /// </summary>
    /// <example>
    /// "PolicyCreated", "RecordTracked", "EnforcementExecuted", "RecordDeleted",
    /// "LegalHoldApplied", "LegalHoldReleased", "ExpirationAlertSent"
    /// </example>
    public required string Action { get; init; }

    /// <summary>
    /// Identifier of the data entity affected by this action.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for actions that are not entity-specific, such as
    /// enforcement cycle summaries or policy-level changes.
    /// </remarks>
    public string? EntityId { get; init; }

    /// <summary>
    /// The data category affected by this action.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for actions that span multiple categories, such as
    /// enforcement execution summaries.
    /// </remarks>
    public string? DataCategory { get; init; }

    /// <summary>
    /// Additional details about the action performed.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when no additional context is needed. For enforcement actions,
    /// this may contain a summary (e.g., "Deleted 42 records, retained 3 under legal hold").
    /// For legal holds, this may contain the hold reason.
    /// </remarks>
    public string? Detail { get; init; }

    /// <summary>
    /// Identifier of the user or system that performed the action.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for automated system actions (e.g., scheduled enforcement,
    /// automatic record tracking by the pipeline behavior).
    /// </remarks>
    public string? PerformedByUserId { get; init; }

    /// <summary>
    /// Timestamp when the action occurred (UTC).
    /// </summary>
    public required DateTimeOffset OccurredAtUtc { get; init; }

    /// <summary>
    /// Creates a new retention audit entry with a generated unique identifier
    /// and the current UTC timestamp.
    /// </summary>
    /// <param name="action">The action that was performed.</param>
    /// <param name="entityId">Identifier of the affected entity, if applicable.</param>
    /// <param name="dataCategory">The affected data category, if applicable.</param>
    /// <param name="detail">Additional details about the action.</param>
    /// <param name="performedByUserId">Identifier of the actor who performed the action.</param>
    /// <returns>A new <see cref="RetentionAuditEntry"/> with a generated GUID identifier.</returns>
    public static RetentionAuditEntry Create(
        string action,
        string? entityId = null,
        string? dataCategory = null,
        string? detail = null,
        string? performedByUserId = null) =>
        new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Action = action,
            EntityId = entityId,
            DataCategory = dataCategory,
            Detail = detail,
            PerformedByUserId = performedByUserId,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
}
