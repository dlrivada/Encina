using Encina.Compliance.ProcessorAgreements.Model;

using LanguageExt;

namespace Encina.Compliance.ProcessorAgreements;

/// <summary>
/// Store for recording and retrieving audit trail entries for processor and DPA operations.
/// </summary>
/// <remarks>
/// <para>
/// The audit store provides an immutable, append-only record of all actions taken on
/// processors and their Data Processing Agreements. This supports the accountability
/// principle under GDPR Article 5(2), which requires the controller to be able to
/// demonstrate compliance with the regulation.
/// </para>
/// <para>
/// Audit entries are recorded for:
/// </para>
/// <list type="bullet">
/// <item><description>Processor registration, update, and removal.</description></item>
/// <item><description>Sub-processor addition and removal (with depth tracking per Article 28(2)).</description></item>
/// <item><description>DPA creation, status changes, termination, and expiration.</description></item>
/// <item><description>DPA validation results (Article 28(3) compliance checks).</description></item>
/// <item><description>Pipeline behavior enforcement actions (block, warn).</description></item>
/// </list>
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
/// // Record an audit entry for processor registration
/// var entry = new ProcessorAgreementAuditEntry
/// {
///     Id = Guid.NewGuid().ToString(),
///     ProcessorId = "stripe-payments",
///     Action = "Registered",
///     Detail = "Processor registered with general sub-processor authorization.",
///     OccurredAtUtc = DateTimeOffset.UtcNow
/// };
/// await auditStore.RecordAsync(entry, ct);
///
/// // Retrieve the full audit trail for a processor
/// var trail = await auditStore.GetAuditTrailAsync("stripe-payments", ct);
/// </code>
/// </example>
public interface IProcessorAuditStore
{
    /// <summary>
    /// Records a new audit entry for a processor or DPA action.
    /// </summary>
    /// <param name="entry">The audit entry to persist.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the entry
    /// could not be stored.
    /// </returns>
    /// <remarks>
    /// Audit entries are append-only. Once recorded, they should not be modified or deleted
    /// to maintain the integrity of the audit trail per the accountability principle (Article 5(2)).
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> RecordAsync(
        ProcessorAgreementAuditEntry entry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the complete audit trail for a specific processor.
    /// </summary>
    /// <param name="processorId">The unique identifier of the processor.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of audit entries ordered chronologically (oldest first),
    /// or an <see cref="EncinaError"/> on failure. Returns an empty list if no
    /// entries exist for the given processor.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The audit trail includes entries for both processor-level operations (registration,
    /// update, removal, sub-processor changes) and DPA-level operations (signing, termination,
    /// expiration, validation). Entries are correlated by <see cref="ProcessorAgreementAuditEntry.ProcessorId"/>
    /// and optionally by <see cref="ProcessorAgreementAuditEntry.DPAId"/>.
    /// </para>
    /// <para>
    /// Entries are returned in chronological order by <see cref="ProcessorAgreementAuditEntry.OccurredAtUtc"/>
    /// to facilitate timeline reconstruction for regulatory audits and compliance reviews.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<ProcessorAgreementAuditEntry>>> GetAuditTrailAsync(
        string processorId,
        CancellationToken cancellationToken = default);
}
