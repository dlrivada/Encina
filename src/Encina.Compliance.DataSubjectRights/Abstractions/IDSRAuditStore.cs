using LanguageExt;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Store for recording and querying the DSR audit trail.
/// </summary>
/// <remarks>
/// <para>
/// The audit store provides an immutable record of all actions taken during DSR request
/// processing. This audit trail is essential for demonstrating compliance with GDPR
/// obligations under the accountability principle (Article 5(2)).
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// <para>
/// Audit entries cover the entire request lifecycle: receipt, identity verification,
/// processing steps, completion or rejection, deadline extensions, and third-party
/// notifications (Article 19).
/// </para>
/// <para>
/// Implementations may store audit entries in-memory (for development/testing), in a database
/// (for production), or in any other suitable backing store. All 13 database providers
/// are supported: ADO.NET (4), Dapper (4), EF Core (4), and MongoDB (1).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Record an audit entry
/// var entry = new DSRAuditEntry
/// {
///     Id = Guid.NewGuid().ToString(),
///     DSRRequestId = "req-001",
///     Action = "ErasureExecuted",
///     Detail = "Erased 12 fields across 3 entities",
///     PerformedByUserId = "admin-456",
///     OccurredAtUtc = DateTimeOffset.UtcNow
/// };
///
/// await store.RecordAsync(entry, cancellationToken);
///
/// // Retrieve the audit trail for a request
/// var trail = await store.GetAuditTrailAsync("req-001", cancellationToken);
/// </code>
/// </example>
public interface IDSRAuditStore
{
    /// <summary>
    /// Records a new audit entry for a DSR request action.
    /// </summary>
    /// <param name="entry">The audit entry to persist.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the entry
    /// could not be recorded.
    /// </returns>
    /// <remarks>
    /// Audit entries are immutable once recorded. Implementations must not allow
    /// modification or deletion of existing entries.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> RecordAsync(
        DSRAuditEntry entry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the complete audit trail for a specific DSR request.
    /// </summary>
    /// <param name="dsrRequestId">The identifier of the DSR request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of audit entries ordered chronologically by
    /// <see cref="DSRAuditEntry.OccurredAtUtc"/>, or an <see cref="EncinaError"/> on failure.
    /// Returns an empty list if no entries exist for the given request ID.
    /// </returns>
    /// <remarks>
    /// The audit trail provides evidence of compliance with GDPR obligations including
    /// response times (Article 12(3)), notification of recipients (Article 19),
    /// and identity verification procedures.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<DSRAuditEntry>>> GetAuditTrailAsync(
        string dsrRequestId,
        CancellationToken cancellationToken = default);
}
