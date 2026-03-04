using Encina.Compliance.BreachNotification.Model;

using LanguageExt;

namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Store for recording and querying the breach notification audit trail.
/// </summary>
/// <remarks>
/// <para>
/// GDPR Article 33(5) requires the controller to document "any personal data breaches,
/// comprising the facts relating to the personal data breach, its effects and the
/// remedial action taken." The breach audit store provides an immutable record of all
/// actions taken during the breach notification lifecycle.
/// </para>
/// <para>
/// Per GDPR Article 5(2) (accountability principle), controllers must demonstrate
/// compliance with data protection principles. Breach audit entries provide evidence
/// of timely detection, notification, and resolution actions.
/// </para>
/// <para>
/// Audit entries should never be modified or deleted. They serve as legal evidence
/// of the notification measures applied and may be required during regulatory audits
/// or supervisory authority inquiries (Article 58).
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// <para>
/// Implementations may store entries in-memory (for development/testing), in a database
/// (for production), or in any other suitable backing store. All 13 database providers
/// are supported: ADO.NET (4), Dapper (4), EF Core (4), and MongoDB (1).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Record a notification action in the audit trail
/// var entry = BreachAuditEntry.Create(
///     breachId: "abc123",
///     action: "AuthorityNotified",
///     detail: "Supervisory authority notified via email within 48 hours of detection",
///     performedByUserId: "dpo-user-001");
///
/// await auditStore.RecordAsync(entry, cancellationToken);
///
/// // Retrieve the complete audit trail for a breach
/// var trail = await auditStore.GetAuditTrailAsync("abc123", cancellationToken);
/// </code>
/// </example>
public interface IBreachAuditStore
{
    /// <summary>
    /// Records a new entry in the breach notification audit trail.
    /// </summary>
    /// <param name="entry">The audit entry to record.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the entry
    /// could not be recorded.
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> RecordAsync(
        BreachAuditEntry entry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the complete audit trail for a specific breach.
    /// </summary>
    /// <param name="breachId">The identifier of the breach.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of audit entries for the breach ordered chronologically,
    /// or an <see cref="EncinaError"/> on failure. Returns an empty list if no entries
    /// exist for the breach.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Provides a complete, chronological history of all actions taken for a breach,
    /// from detection through resolution. This supports the documentation requirement
    /// of Article 33(5) and the accountability principle of Article 5(2).
    /// </para>
    /// <para>
    /// The audit trail is the primary evidence artifact during supervisory authority
    /// inquiries and compliance audits.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<BreachAuditEntry>>> GetAuditTrailAsync(
        string breachId,
        CancellationToken cancellationToken = default);
}
