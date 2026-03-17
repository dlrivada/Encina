using Encina.Compliance.Retention.Model;
using Encina.Compliance.Retention.ReadModels;
using LanguageExt;

namespace Encina.Compliance.Retention.Abstractions;

/// <summary>
/// Service interface for managing retention record lifecycle operations via event-sourced aggregates.
/// </summary>
/// <remarks>
/// <para>
/// Provides a clean API for tracking, expiring, deleting, anonymizing, and querying retention records.
/// The implementation wraps the event-sourced <c>RetentionRecordAggregate</c> via
/// <c>IAggregateRepository&lt;RetentionRecordAggregate&gt;</c>, handling aggregate loading,
/// command execution, persistence, and cache management.
/// </para>
/// <para>
/// This service replaces the legacy <c>IRetentionRecordStore</c> interface with a CQRS-oriented API.
/// The event stream serves as the audit trail, eliminating the need for separate audit recording
/// per GDPR Article 5(2) accountability.
/// </para>
/// <para>
/// <b>Commands</b> (write operations via aggregate):
/// <list type="bullet">
///   <item><description><see cref="TrackEntityAsync"/> — Begins tracking a data entity for retention (Art. 5(1)(e))</description></item>
///   <item><description><see cref="MarkExpiredAsync"/> — Marks a record as expired when retention period elapses</description></item>
///   <item><description><see cref="HoldRecordAsync"/> — Places a legal hold on a record (Art. 17(3)(e))</description></item>
///   <item><description><see cref="ReleaseRecordAsync"/> — Releases a legal hold from a record</description></item>
///   <item><description><see cref="MarkDeletedAsync"/> — Records physical data deletion (Art. 17(1)(a))</description></item>
///   <item><description><see cref="MarkAnonymizedAsync"/> — Records data anonymization (Recital 26)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Queries</b> (read operations via read model repository):
/// <list type="bullet">
///   <item><description><see cref="GetRecordAsync"/> — Retrieves a record by ID</description></item>
///   <item><description><see cref="GetRecordsByEntityAsync"/> — Lists records for a specific entity</description></item>
///   <item><description><see cref="GetRecordsByStatusAsync"/> — Lists records by lifecycle status</description></item>
///   <item><description><see cref="GetExpiredRecordsAsync"/> — Finds records eligible for deletion</description></item>
///   <item><description><see cref="GetRecordsByPolicyAsync"/> — Lists records governed by a specific policy</description></item>
///   <item><description><see cref="GetRecordHistoryAsync"/> — Retrieves full event history</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IRetentionRecordService
{
    // ========================================================================
    // Command operations (write-side via RetentionRecordAggregate)
    // ========================================================================

    /// <summary>
    /// Begins tracking a data entity for retention under a specific policy.
    /// </summary>
    /// <param name="entityId">The identifier of the data entity being tracked.</param>
    /// <param name="dataCategory">The data category this entity belongs to.</param>
    /// <param name="policyId">The retention policy aggregate ID governing this entity.</param>
    /// <param name="retentionPeriod">The retention period copied from the policy.</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the identifier of the newly created retention record aggregate.</returns>
    /// <remarks>
    /// The expiration timestamp is computed as <c>now + retentionPeriod</c> using the injected
    /// <see cref="TimeProvider"/>. Per GDPR Article 5(1)(e), each tracked entity has a defined
    /// retention period after which it becomes eligible for deletion.
    /// </remarks>
    ValueTask<Either<EncinaError, Guid>> TrackEntityAsync(
        string entityId,
        string dataCategory,
        Guid policyId,
        TimeSpan retentionPeriod,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a retention record as expired, indicating the retention period has elapsed.
    /// </summary>
    /// <param name="recordId">The retention record aggregate identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// Only records in <see cref="RetentionStatus.Active"/> status can be marked as expired.
    /// The entity is now eligible for deletion unless a legal hold prevents it.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> MarkExpiredAsync(
        Guid recordId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Places a legal hold on a retention record, suspending deletion.
    /// </summary>
    /// <param name="recordId">The retention record aggregate identifier.</param>
    /// <param name="legalHoldId">The identifier of the legal hold aggregate that triggered this hold.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// Per GDPR Article 17(3)(e), legal holds suspend deletion for entities under litigation
    /// or legal proceedings. A record can be held from Active or Expired status.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> HoldRecordAsync(
        Guid recordId,
        Guid legalHoldId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a legal hold from a retention record, resuming normal lifecycle processing.
    /// </summary>
    /// <param name="recordId">The retention record aggregate identifier.</param>
    /// <param name="legalHoldId">The identifier of the legal hold aggregate that was lifted.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// The record's status is recalculated based on the current time relative to the expiration
    /// timestamp. The enforcement service will re-evaluate the record during its next sweep.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> ReleaseRecordAsync(
        Guid recordId,
        Guid legalHoldId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a retention record's data entity as physically deleted.
    /// </summary>
    /// <param name="recordId">The retention record aggregate identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// This is a terminal state — no further state changes are permitted. The event provides
    /// an immutable audit trail proving that data was deleted in compliance with GDPR
    /// Article 5(1)(e) storage limitation and Article 17(1) right to erasure.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> MarkDeletedAsync(
        Guid recordId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a retention record's data entity as anonymized.
    /// </summary>
    /// <param name="recordId">The retention record aggregate identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// This is a terminal state — no further state changes are permitted. Per GDPR Recital 26,
    /// anonymized data falls outside GDPR scope. Anonymization is an alternative to deletion
    /// where the underlying record structure is preserved.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> MarkAnonymizedAsync(
        Guid recordId,
        CancellationToken cancellationToken = default);

    // ========================================================================
    // Query operations (read-side via RetentionRecordReadModel)
    // ========================================================================

    /// <summary>
    /// Retrieves a retention record by its aggregate identifier.
    /// </summary>
    /// <param name="recordId">The retention record aggregate identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error (including not-found) or the retention record read model.</returns>
    ValueTask<Either<EncinaError, RetentionRecordReadModel>> GetRecordAsync(
        Guid recordId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all retention records for a specific data entity.
    /// </summary>
    /// <param name="entityId">The identifier of the data entity.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only list of retention record read models for the entity.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecordReadModel>>> GetRecordsByEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all retention records with a given lifecycle status.
    /// </summary>
    /// <param name="status">The retention status to filter by.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only list of retention record read models matching the status.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecordReadModel>>> GetRecordsByStatusAsync(
        RetentionStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves active retention records that have exceeded their expiration time.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// A read-only list of retention record read models where the status is
    /// <see cref="RetentionStatus.Active"/> and <see cref="RetentionRecordReadModel.ExpiresAtUtc"/>
    /// is in the past. These records are eligible for deletion by the enforcement service.
    /// </returns>
    ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecordReadModel>>> GetExpiredRecordsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all retention records governed by a specific retention policy.
    /// </summary>
    /// <param name="policyId">The retention policy aggregate identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only list of retention record read models for the policy.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecordReadModel>>> GetRecordsByPolicyAsync(
        Guid policyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the full event history for a retention record aggregate.
    /// </summary>
    /// <param name="recordId">The retention record aggregate identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// Either an error or the list of domain events that have been applied to this record,
    /// ordered chronologically. Provides a complete audit trail for GDPR Article 5(2) accountability.
    /// </returns>
    ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetRecordHistoryAsync(
        Guid recordId,
        CancellationToken cancellationToken = default);
}
