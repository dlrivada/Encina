using Encina.Compliance.Retention.Model;

using LanguageExt;

namespace Encina.Compliance.Retention;

/// <summary>
/// Service for managing the lifecycle of legal holds (litigation holds).
/// </summary>
/// <remarks>
/// <para>
/// The legal hold manager coordinates hold application and release across multiple stores,
/// ensuring consistent state between <see cref="ILegalHoldStore"/>,
/// <see cref="IRetentionRecordStore"/>, and <see cref="IRetentionAuditStore"/>.
/// It provides the application-level logic that sits above raw store operations.
/// </para>
/// <para>
/// Per GDPR Article 17(3)(e), the right to erasure does not apply when processing is
/// necessary for the establishment, exercise, or defence of legal claims. The legal hold
/// manager implements this exemption by suspending automatic deletion for entities under hold,
/// regardless of their retention period status.
/// </para>
/// <para>
/// When a hold is applied:
/// <list type="number">
/// <item><description>The <see cref="LegalHold"/> record is persisted via <see cref="ILegalHoldStore"/>.</description></item>
/// <item><description>Matching <see cref="RetentionRecord"/> entries are updated to <see cref="RetentionStatus.UnderLegalHold"/>.</description></item>
/// <item><description>An audit entry is recorded via <see cref="IRetentionAuditStore"/>.</description></item>
/// <item><description>A <see cref="LegalHoldAppliedNotification"/> is published.</description></item>
/// </list>
/// </para>
/// <para>
/// When a hold is released:
/// <list type="number">
/// <item><description>The <see cref="LegalHold"/> record is updated with release metadata.</description></item>
/// <item><description>Matching records revert to <see cref="RetentionStatus.Expired"/> (if past expiration) or <see cref="RetentionStatus.Active"/>.</description></item>
/// <item><description>An audit entry is recorded.</description></item>
/// <item><description>A <see cref="LegalHoldReleasedNotification"/> is published.</description></item>
/// </list>
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Apply a legal hold to prevent deletion during litigation
/// var hold = LegalHold.Create(
///     entityId: "invoice-12345",
///     reason: "Pending tax audit for fiscal year 2024",
///     appliedByUserId: "legal-counsel@company.com");
///
/// await holdManager.ApplyHoldAsync("invoice-12345", hold, cancellationToken);
///
/// // Check if an entity is under hold before manual operations
/// var isHeld = await holdManager.IsUnderHoldAsync("invoice-12345", cancellationToken);
///
/// // Release the hold when litigation concludes
/// await holdManager.ReleaseHoldAsync(hold.Id, "legal-counsel@company.com", cancellationToken);
/// </code>
/// </example>
public interface ILegalHoldManager
{
    /// <summary>
    /// Applies a legal hold to a data entity, suspending automatic deletion.
    /// </summary>
    /// <param name="entityId">The identifier of the data entity to hold.</param>
    /// <param name="hold">The legal hold to apply.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the hold
    /// could not be applied (e.g., an active hold already exists for the entity).
    /// </returns>
    /// <remarks>
    /// Applying a hold also updates any matching <see cref="RetentionRecord"/> entries
    /// for the entity to <see cref="RetentionStatus.UnderLegalHold"/> status, preventing
    /// the retention enforcer from deleting the data during enforcement cycles.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> ApplyHoldAsync(
        string entityId,
        LegalHold hold,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a legal hold, re-enabling deletion eligibility for the associated data entity.
    /// </summary>
    /// <param name="holdId">The unique identifier of the hold to release.</param>
    /// <param name="releasedByUserId">Identifier of the user releasing the hold. <c>null</c> for system-initiated releases.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the hold
    /// was not found or was already released.
    /// </returns>
    /// <remarks>
    /// <para>
    /// After release, matching <see cref="RetentionRecord"/> entries revert to
    /// <see cref="RetentionStatus.Expired"/> (if the retention period has passed) or
    /// <see cref="RetentionStatus.Active"/> (if still within period). The entity becomes
    /// eligible for deletion in the next enforcement cycle if expired.
    /// </para>
    /// <para>
    /// Only the last remaining active hold release reverts the retention record status.
    /// If multiple holds exist for the same entity, the status remains
    /// <see cref="RetentionStatus.UnderLegalHold"/> until all holds are released.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> ReleaseHoldAsync(
        string holdId,
        string? releasedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the specified data entity has any active legal holds.
    /// </summary>
    /// <param name="entityId">The identifier of the data entity.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if any active hold exists for the entity (i.e., a hold where
    /// <see cref="LegalHold.ReleasedAtUtc"/> is <c>null</c>), <c>false</c> otherwise,
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// Delegates to <see cref="ILegalHoldStore.IsUnderHoldAsync"/>. This is a convenience
    /// method for application code that needs to check hold status without interacting
    /// with the store directly.
    /// </remarks>
    ValueTask<Either<EncinaError, bool>> IsUnderHoldAsync(
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all currently active legal holds across all entities.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of active legal holds (where <see cref="LegalHold.ReleasedAtUtc"/>
    /// is <c>null</c>), or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// Used for compliance dashboards, legal team reporting, and generating
    /// hold inventories required during regulatory audits. Delegates to
    /// <see cref="ILegalHoldStore.GetActiveHoldsAsync"/>.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<LegalHold>>> GetActiveHoldsAsync(
        CancellationToken cancellationToken = default);
}
