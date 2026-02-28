using Encina.Compliance.Anonymization.Model;

using LanguageExt;

namespace Encina.Compliance.Anonymization;

/// <summary>
/// Store for recording and querying anonymization audit trail entries.
/// </summary>
/// <remarks>
/// <para>
/// GDPR Article 5(2) (accountability principle) requires controllers to demonstrate
/// compliance with data protection principles. The audit store provides an immutable
/// record of all anonymization, pseudonymization, tokenization, and key management
/// actions to support this demonstrability requirement.
/// </para>
/// <para>
/// Audit entries should never be modified or deleted. They serve as legal evidence
/// of the data protection measures applied and may be required during regulatory
/// audits or data subject access requests (Article 15).
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
/// // Record an anonymization action in the audit trail
/// var entry = AnonymizationAuditEntry.Create(
///     operation: AnonymizationOperation.Anonymized,
///     technique: AnonymizationTechnique.Generalization,
///     fieldName: "Age",
///     subjectId: "user-123",
///     performedByUserId: "system");
///
/// await auditStore.AddEntryAsync(entry, cancellationToken);
///
/// // Retrieve audit trail for a specific data subject
/// var trail = await auditStore.GetBySubjectIdAsync("user-123", cancellationToken);
/// </code>
/// </example>
public interface IAnonymizationAuditStore
{
    /// <summary>
    /// Records a new audit entry in the anonymization audit trail.
    /// </summary>
    /// <param name="entry">The audit entry to record.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the entry
    /// could not be recorded.
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> AddEntryAsync(
        AnonymizationAuditEntry entry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the anonymization audit trail for a specific data subject.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of audit entries ordered by <see cref="AnonymizationAuditEntry.PerformedAtUtc"/>
    /// descending (most recent first), or an <see cref="EncinaError"/> on failure.
    /// Returns an empty list if no entries exist for the subject.
    /// </returns>
    /// <remarks>
    /// This method supports data subject access requests (Article 15) by providing
    /// a complete history of anonymization actions for a given individual.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<AnonymizationAuditEntry>>> GetBySubjectIdAsync(
        string subjectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all anonymization audit entries across all subjects.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of all audit entries, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// Primarily used for compliance reporting and regulatory audits. For large datasets,
    /// consider implementing pagination in provider-specific extensions.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<AnonymizationAuditEntry>>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
