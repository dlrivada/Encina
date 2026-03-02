using Encina.Compliance.DataResidency.Model;

using LanguageExt;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Store for recording and querying the data residency audit trail.
/// </summary>
/// <remarks>
/// <para>
/// GDPR Article 5(2) (accountability principle) requires controllers to demonstrate
/// compliance with data protection principles. Per Article 30, controllers must maintain
/// records of processing activities including transfers of personal data to a third country.
/// The residency audit store provides an immutable record of all residency enforcement
/// decisions, cross-border transfer validations, region routing outcomes, and policy violations.
/// </para>
/// <para>
/// Audit entries should never be modified or deleted. They serve as legal evidence
/// of data residency compliance and may be required during regulatory audits or
/// supervisory authority inquiries (Article 58).
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
/// // Record a cross-border transfer validation in the audit trail
/// var entry = ResidencyAuditEntry.Create(
///     dataCategory: "personal-data",
///     sourceRegion: "DE",
///     action: ResidencyAction.CrossBorderTransfer,
///     outcome: ResidencyOutcome.Allowed,
///     targetRegion: "US",
///     legalBasis: "StandardContractualClauses",
///     details: "Transfer approved with SCC safeguards");
///
/// await auditStore.RecordAsync(entry, cancellationToken);
///
/// // Retrieve audit trail for a specific entity
/// var trail = await auditStore.GetByEntityAsync("customer-42", cancellationToken);
/// </code>
/// </example>
public interface IResidencyAuditStore
{
    /// <summary>
    /// Records a new entry in the residency audit trail.
    /// </summary>
    /// <param name="entry">The audit entry to record.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the entry
    /// could not be recorded.
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> RecordAsync(
        ResidencyAuditEntry entry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the residency audit trail for a specific data entity.
    /// </summary>
    /// <param name="entityId">The identifier of the data entity.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of audit entries for the entity, or an <see cref="EncinaError"/>
    /// on failure. Returns an empty list if no entries exist for the entity.
    /// </returns>
    /// <remarks>
    /// Provides a complete history of residency actions for a data entity,
    /// supporting accountability demonstrations per Article 5(2).
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetByEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves residency audit entries within a specific date range.
    /// </summary>
    /// <param name="fromUtc">The start of the date range (inclusive, UTC).</param>
    /// <param name="toUtc">The end of the date range (inclusive, UTC).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of audit entries within the specified date range,
    /// or an <see cref="EncinaError"/> on failure. Returns an empty list if no entries
    /// fall within the range.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Filters entries by <see cref="ResidencyAuditEntry.TimestampUtc"/>. Useful for
    /// generating periodic compliance reports and for responding to supervisory authority
    /// inquiries about data transfers during a specific time period (Article 58).
    /// </para>
    /// <para>
    /// Both <paramref name="fromUtc"/> and <paramref name="toUtc"/> are inclusive boundaries.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetByDateRangeAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all residency audit entries that recorded violations.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of violation audit entries (where <see cref="ResidencyAuditEntry.Outcome"/>
    /// is <see cref="ResidencyOutcome.Blocked"/>), or an <see cref="EncinaError"/> on failure.
    /// Returns an empty list if no violations have been recorded.
    /// </returns>
    /// <remarks>
    /// Violations represent attempts to store or transfer data to non-compliant regions
    /// that were blocked by the residency enforcement system. This method provides a
    /// quick overview of all compliance incidents for security reviews and DPO reporting.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetViolationsAsync(
        CancellationToken cancellationToken = default);
}
