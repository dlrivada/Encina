using Encina.Compliance.DPIA.Model;

using LanguageExt;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Store for recording and retrieving DPIA audit trail entries.
/// </summary>
/// <remarks>
/// <para>
/// The audit store provides an immutable, append-only record of all actions taken on
/// DPIA assessments. This supports the accountability principle under GDPR Article 5(2),
/// which requires the controller to be able to demonstrate compliance.
/// </para>
/// <para>
/// Every lifecycle event in the DPIA process generates an audit entry: creation, risk
/// evaluation, DPO consultation requests and responses, approvals, rejections, reviews,
/// and expirations. The audit trail provides a complete, chronological record for
/// regulatory audits and internal governance.
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
/// // Record an audit entry for assessment creation
/// var entry = new DPIAAuditEntry
/// {
///     Id = Guid.NewGuid(),
///     AssessmentId = assessmentId,
///     Action = "Created",
///     PerformedBy = "system",
///     OccurredAtUtc = DateTimeOffset.UtcNow,
///     Details = "DPIA assessment initiated for ProcessBiometricDataCommand."
/// };
/// await auditStore.RecordAuditEntryAsync(entry, ct);
///
/// // Retrieve the full audit trail for an assessment
/// var trail = await auditStore.GetAuditTrailAsync(assessmentId, ct);
/// </code>
/// </example>
public interface IDPIAAuditStore
{
    /// <summary>
    /// Records a new audit entry for a DPIA assessment action.
    /// </summary>
    /// <param name="entry">The audit entry to persist.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the entry
    /// could not be stored.
    /// </returns>
    /// <remarks>
    /// Audit entries are append-only. Once recorded, they should not be modified or deleted
    /// to maintain the integrity of the audit trail per the accountability principle.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> RecordAuditEntryAsync(
        DPIAAuditEntry entry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the complete audit trail for a specific DPIA assessment.
    /// </summary>
    /// <param name="assessmentId">The unique identifier of the assessment.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of audit entries ordered chronologically (oldest first),
    /// or an <see cref="EncinaError"/> on failure. Returns an empty list if no
    /// entries exist for the given assessment.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The audit trail provides a complete record of the assessment lifecycle,
    /// supporting regulatory audits and internal compliance reviews.
    /// </para>
    /// <para>
    /// Entries are returned in chronological order by <see cref="DPIAAuditEntry.OccurredAtUtc"/>
    /// to facilitate timeline reconstruction.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<DPIAAuditEntry>>> GetAuditTrailAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default);
}
