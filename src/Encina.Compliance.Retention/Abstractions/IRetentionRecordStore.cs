using Encina.Compliance.Retention.Model;

using LanguageExt;

namespace Encina.Compliance.Retention;

/// <summary>
/// Store for tracking the retention lifecycle of individual data entities.
/// </summary>
/// <remarks>
/// <para>
/// The retention record store manages <see cref="RetentionRecord"/> instances that track
/// when data was created, when it expires, and its current lifecycle status. Records are
/// the operational backbone of the retention enforcement process.
/// </para>
/// <para>
/// Per GDPR Article 5(1)(e) (storage limitation), this store enables controllers to
/// demonstrate that personal data is not kept longer than necessary by maintaining an
/// auditable trail of data creation, expiration, and deletion timestamps.
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// <para>
/// Implementations may store records in-memory (for development/testing), in a database
/// (for production), or in any other suitable backing store. All 13 database providers
/// are supported: ADO.NET (4), Dapper (4), EF Core (4), and MongoDB (1).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a retention record for a new data entity
/// var record = RetentionRecord.Create(
///     entityId: "order-12345",
///     dataCategory: "financial-records",
///     createdAtUtc: DateTimeOffset.UtcNow,
///     expiresAtUtc: DateTimeOffset.UtcNow.AddYears(7),
///     policyId: "policy-001");
///
/// await recordStore.CreateAsync(record, cancellationToken);
///
/// // Query expired records for enforcement
/// var expired = await recordStore.GetExpiredRecordsAsync(cancellationToken);
/// </code>
/// </example>
public interface IRetentionRecordStore
{
    /// <summary>
    /// Creates a new retention record.
    /// </summary>
    /// <param name="record">The retention record to persist.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the record
    /// could not be stored (e.g., duplicate ID).
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> CreateAsync(
        RetentionRecord record,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a retention record by its unique identifier.
    /// </summary>
    /// <param name="recordId">The unique identifier of the record.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>Some(record)</c> if a record with the given ID exists,
    /// <c>None</c> if no record is found, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, Option<RetentionRecord>>> GetByIdAsync(
        string recordId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all retention records for a specific data entity.
    /// </summary>
    /// <param name="entityId">The identifier of the data entity.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of retention records for the entity, or an <see cref="EncinaError"/>
    /// on failure. Returns an empty list if no records exist for the entity.
    /// </returns>
    /// <remarks>
    /// A single entity may have multiple retention records if it belongs to multiple
    /// data categories (e.g., an invoice may be tracked under both "financial-records"
    /// and "customer-data").
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecord>>> GetByEntityIdAsync(
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all retention records that have expired and are eligible for deletion.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of expired retention records, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Returns records where <see cref="RetentionRecord.ExpiresAtUtc"/> is in the past
    /// AND <see cref="RetentionRecord.Status"/> is <see cref="RetentionStatus.Active"/>.
    /// Records under legal hold or already deleted are excluded.
    /// </para>
    /// <para>
    /// This is the primary query used by <c>RetentionEnforcementService</c> during
    /// enforcement cycles to identify data eligible for automatic deletion.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecord>>> GetExpiredRecordsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all retention records that will expire within the specified time window.
    /// </summary>
    /// <param name="within">The time window to look ahead for upcoming expirations.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of retention records expiring within the specified window,
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Returns records where <see cref="RetentionRecord.ExpiresAtUtc"/> is between now
    /// and now + <paramref name="within"/>, AND <see cref="RetentionRecord.Status"/> is
    /// <see cref="RetentionStatus.Active"/>. Used to generate expiration alerts.
    /// </para>
    /// <para>
    /// Per Recital 39, appropriate measures should include establishing time limits
    /// for erasure or periodic review. This method supports proactive notification
    /// before data reaches its retention deadline.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecord>>> GetExpiringWithinAsync(
        TimeSpan within,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the lifecycle status of a retention record.
    /// </summary>
    /// <param name="recordId">The unique identifier of the record to update.</param>
    /// <param name="newStatus">The new lifecycle status.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the record
    /// was not found or the status transition is invalid.
    /// </returns>
    /// <remarks>
    /// Valid status transitions:
    /// <list type="bullet">
    /// <item><description><c>Active → Expired</c> — retention period has elapsed.</description></item>
    /// <item><description><c>Active → UnderLegalHold</c> — legal hold applied.</description></item>
    /// <item><description><c>Expired → Deleted</c> — data successfully deleted.</description></item>
    /// <item><description><c>UnderLegalHold → Active</c> — legal hold released, data still within period.</description></item>
    /// <item><description><c>UnderLegalHold → Expired</c> — legal hold released, data past expiration.</description></item>
    /// </list>
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> UpdateStatusAsync(
        string recordId,
        RetentionStatus newStatus,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all retention records.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of all retention records, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// Primarily used for compliance reporting and auditing. For large datasets, consider
    /// implementing pagination in provider-specific extensions.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecord>>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
