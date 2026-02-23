using LanguageExt;

namespace Encina.Compliance.Consent;

/// <summary>
/// Store for recording and querying consent audit trail entries.
/// </summary>
/// <remarks>
/// <para>
/// GDPR Article 7(1) requires controllers to demonstrate that data subjects have
/// consented to the processing of their personal data. The audit store provides an
/// immutable record of all consent-related actions to support this demonstrability
/// requirement.
/// </para>
/// <para>
/// Audit entries should never be modified or deleted. They serve as legal evidence
/// of consent management and may be required during regulatory audits or data subject
/// access requests (Article 15).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Record a consent grant in the audit trail
/// var entry = new ConsentAuditEntry
/// {
///     Id = Guid.NewGuid(),
///     SubjectId = "user-123",
///     Purpose = ConsentPurposes.Marketing,
///     Action = ConsentAuditAction.Granted,
///     OccurredAtUtc = DateTimeOffset.UtcNow,
///     PerformedBy = "user-123",
///     IpAddress = "192.168.1.1",
///     Metadata = new Dictionary&lt;string, object?&gt;
///     {
///         ["consentVersionId"] = "marketing-v2",
///         ["source"] = "web-form"
///     }
/// };
///
/// await auditStore.RecordAsync(entry, cancellationToken);
/// </code>
/// </example>
public interface IConsentAuditStore
{
    /// <summary>
    /// Records a new audit entry in the consent audit trail.
    /// </summary>
    /// <param name="entry">The audit entry to record.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the entry
    /// could not be recorded.
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> RecordAsync(
        ConsentAuditEntry entry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the consent audit trail for a data subject, optionally filtered by purpose.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="purpose">
    /// Optional processing purpose filter. When <c>null</c>, returns audit entries
    /// for all purposes.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of audit entries ordered by <see cref="ConsentAuditEntry.OccurredAtUtc"/>
    /// descending (most recent first), or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// This method supports data subject access requests (Article 15) by providing
    /// a complete history of consent actions for a given individual.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<ConsentAuditEntry>>> GetAuditTrailAsync(
        string subjectId,
        string? purpose = null,
        CancellationToken cancellationToken = default);
}
