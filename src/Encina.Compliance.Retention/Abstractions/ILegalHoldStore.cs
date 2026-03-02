using Encina.Compliance.Retention.Model;

using LanguageExt;

namespace Encina.Compliance.Retention;

/// <summary>
/// Store for managing legal hold (litigation hold) persistence and queries.
/// </summary>
/// <remarks>
/// <para>
/// The legal hold store provides CRUD operations for <see cref="LegalHold"/> records,
/// enabling litigation hold management that suspends data deletion for specific entities.
/// </para>
/// <para>
/// Per GDPR Article 17(3)(e), the right to erasure does not apply when processing is
/// necessary for the establishment, exercise, or defence of legal claims. Legal holds
/// implement this exemption in a controlled, auditable manner.
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// <para>
/// Implementations may store holds in-memory (for development/testing), in a database
/// (for production), or in any other suitable backing store. All 13 database providers
/// are supported: ADO.NET (4), Dapper (4), EF Core (4), and MongoDB (1).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Apply a legal hold to prevent deletion
/// var hold = LegalHold.Create(
///     entityId: "invoice-12345",
///     reason: "Pending tax audit for fiscal year 2024",
///     appliedByUserId: "legal-counsel@company.com");
///
/// await holdStore.CreateAsync(hold, cancellationToken);
///
/// // Check if an entity is under hold before deletion
/// var isHeld = await holdStore.IsUnderHoldAsync("invoice-12345", cancellationToken);
/// </code>
/// </example>
public interface ILegalHoldStore
{
    /// <summary>
    /// Creates a new legal hold record.
    /// </summary>
    /// <param name="hold">The legal hold to persist.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the hold
    /// could not be stored (e.g., duplicate ID).
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> CreateAsync(
        LegalHold hold,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a legal hold by its unique identifier.
    /// </summary>
    /// <param name="holdId">The unique identifier of the legal hold.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>Some(hold)</c> if a hold with the given ID exists,
    /// <c>None</c> if no hold is found, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, Option<LegalHold>>> GetByIdAsync(
        string holdId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all legal holds for a specific data entity.
    /// </summary>
    /// <param name="entityId">The identifier of the data entity.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of legal holds (both active and released) for the entity,
    /// or an <see cref="EncinaError"/> on failure. Returns an empty list if no holds exist.
    /// </returns>
    /// <remarks>
    /// Returns both active and released holds to provide a complete hold history
    /// for compliance auditing.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<LegalHold>>> GetByEntityIdAsync(
        string entityId,
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
    /// <para>
    /// This method is designed to be lightweight and fast as it is called by
    /// <c>IRetentionEnforcer</c> before every deletion attempt during enforcement cycles.
    /// </para>
    /// <para>
    /// An entity is considered under hold if at least one <see cref="LegalHold"/> record
    /// exists where <see cref="LegalHold.IsActive"/> is <c>true</c>.
    /// </para>
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
    /// Used for compliance dashboards, legal team reporting, and enforcement cycle
    /// pre-filtering.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<LegalHold>>> GetActiveHoldsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a legal hold, re-enabling deletion for the associated data entity.
    /// </summary>
    /// <param name="holdId">The unique identifier of the hold to release.</param>
    /// <param name="releasedByUserId">Identifier of the user releasing the hold. <c>null</c> for system-initiated releases.</param>
    /// <param name="releasedAtUtc">Timestamp when the hold was released (UTC).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the hold
    /// was not found or was already released.
    /// </returns>
    /// <remarks>
    /// After release, the associated data entity will be evaluated in the next
    /// enforcement cycle. If the retention period has expired, the data will be
    /// eligible for deletion.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> ReleaseAsync(
        string holdId,
        string? releasedByUserId,
        DateTimeOffset releasedAtUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all legal holds across all entities.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of all legal holds (both active and released),
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// Primarily used for compliance reporting and auditing. For large datasets, consider
    /// implementing pagination in provider-specific extensions.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<LegalHold>>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
