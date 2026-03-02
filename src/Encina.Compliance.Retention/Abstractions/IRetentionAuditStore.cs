using Encina.Compliance.Retention.Model;

using LanguageExt;

namespace Encina.Compliance.Retention;

/// <summary>
/// Store for recording and querying the retention audit trail.
/// </summary>
/// <remarks>
/// <para>
/// GDPR Article 5(2) (accountability principle) requires controllers to demonstrate
/// compliance with data protection principles including storage limitation. The retention
/// audit store provides an immutable record of all retention-related actions: policy
/// changes, record creation, enforcement executions, legal hold application/release,
/// and deletion outcomes.
/// </para>
/// <para>
/// Audit entries should never be modified or deleted. They serve as legal evidence
/// of the data retention measures applied and may be required during regulatory
/// audits or DPIA reviews (Article 35).
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
/// // Record an enforcement action in the audit trail
/// var entry = RetentionAuditEntry.Create(
///     action: "DataDeleted",
///     entityId: "order-12345",
///     dataCategory: "financial-records",
///     detail: "Retention period expired (7 years), auto-deleted by enforcement service",
///     performedByUserId: "system");
///
/// await auditStore.RecordAsync(entry, cancellationToken);
///
/// // Retrieve audit trail for a specific entity
/// var trail = await auditStore.GetByEntityIdAsync("order-12345", cancellationToken);
/// </code>
/// </example>
public interface IRetentionAuditStore
{
    /// <summary>
    /// Records a new entry in the retention audit trail.
    /// </summary>
    /// <param name="entry">The audit entry to record.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the entry
    /// could not be recorded.
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> RecordAsync(
        RetentionAuditEntry entry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the retention audit trail for a specific data entity.
    /// </summary>
    /// <param name="entityId">The identifier of the data entity.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of audit entries for the entity, or an <see cref="EncinaError"/>
    /// on failure. Returns an empty list if no entries exist for the entity.
    /// </returns>
    /// <remarks>
    /// Provides a complete history of retention actions for a data entity,
    /// supporting accountability demonstrations per Article 5(2).
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<RetentionAuditEntry>>> GetByEntityIdAsync(
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all retention audit entries.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of all audit entries, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// Primarily used for compliance reporting and regulatory audits. For large datasets,
    /// consider implementing pagination in provider-specific extensions.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<RetentionAuditEntry>>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
