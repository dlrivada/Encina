using Encina.Compliance.DataResidency.Model;
using Encina.Compliance.DataResidency.ReadModels;
using LanguageExt;

namespace Encina.Compliance.DataResidency.Abstractions;

/// <summary>
/// Service interface for managing data location lifecycle operations via event-sourced aggregates.
/// </summary>
/// <remarks>
/// <para>
/// Provides a clean API for registering, migrating, verifying, removing, and querying data locations.
/// The implementation wraps the event-sourced <c>DataLocationAggregate</c> via <c>IAggregateRepository</c>,
/// handling aggregate loading, command execution, persistence, and cache management.
/// </para>
/// <para>
/// This service replaces the legacy <c>IDataLocationStore</c> with a CQRS-oriented API. The event
/// stream captures the complete data movement history for GDPR Article 30 (records of processing
/// activities), Article 5(2) accountability, and Article 58 supervisory authority inquiries.
/// </para>
/// <para>
/// <b>Commands</b> (write operations via aggregate):
/// <list type="bullet">
///   <item><description><see cref="RegisterLocationAsync"/> — Registers a new data storage location (Art. 30)</description></item>
///   <item><description><see cref="MigrateLocationAsync"/> — Records a data migration between regions (Chapter V)</description></item>
///   <item><description><see cref="VerifyLocationAsync"/> — Records periodic location verification (Art. 5(2))</description></item>
///   <item><description><see cref="RemoveLocationAsync"/> — Marks a location as removed (Art. 17 erasure)</description></item>
///   <item><description><see cref="RemoveByEntityAsync"/> — Removes all locations for an entity</description></item>
///   <item><description><see cref="DetectViolationAsync"/> — Records a sovereignty violation (Art. 33)</description></item>
///   <item><description><see cref="ResolveViolationAsync"/> — Resolves a sovereignty violation</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Queries</b> (read operations via read model repository):
/// <list type="bullet">
///   <item><description><see cref="GetLocationAsync"/> — Retrieves a location by ID</description></item>
///   <item><description><see cref="GetByEntityAsync"/> — Retrieves all locations for an entity</description></item>
///   <item><description><see cref="GetByRegionAsync"/> — Retrieves all locations in a region</description></item>
///   <item><description><see cref="GetByCategoryAsync"/> — Retrieves all locations for a data category</description></item>
///   <item><description><see cref="GetViolationsAsync"/> — Retrieves all locations with active violations</description></item>
///   <item><description><see cref="GetLocationHistoryAsync"/> — Retrieves full event history for a location</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IDataLocationService
{
    // ========================================================================
    // Command operations (write-side via DataLocationAggregate)
    // ========================================================================

    /// <summary>
    /// Registers a new data storage location for an entity.
    /// </summary>
    /// <param name="entityId">Business identifier of the entity whose data is stored.</param>
    /// <param name="dataCategory">Category of personal data stored (e.g., "personal-data").</param>
    /// <param name="regionCode">Region code where the data is stored (ISO 3166-1 alpha-2 or custom).</param>
    /// <param name="storageType">Classification of how the data is stored.</param>
    /// <param name="metadata">Optional key-value metadata about the storage location.</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the identifier of the newly created location aggregate.</returns>
    /// <remarks>
    /// Per GDPR Article 30, controllers must maintain records of processing activities including
    /// the geographic locations where personal data is processed and stored.
    /// </remarks>
    ValueTask<Either<EncinaError, Guid>> RegisterLocationAsync(
        string entityId,
        string dataCategory,
        string regionCode,
        StorageType storageType,
        IReadOnlyDictionary<string, string>? metadata = null,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a data migration from the current region to a new region.
    /// </summary>
    /// <param name="locationId">The data location aggregate identifier.</param>
    /// <param name="newRegionCode">Region code where the data has been migrated to.</param>
    /// <param name="reason">Explanation of why the data was migrated.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// Data migration between regions is a cross-border transfer under GDPR Chapter V.
    /// The caller is responsible for ensuring a valid legal basis exists before migration.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> MigrateLocationAsync(
        Guid locationId,
        string newRegionCode,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a periodic verification that data remains in the expected region.
    /// </summary>
    /// <param name="locationId">The data location aggregate identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// Regular verification helps organizations demonstrate ongoing compliance with data residency
    /// requirements under GDPR Article 5(2) accountability.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> VerifyLocationAsync(
        Guid locationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a data location record, indicating data is no longer stored in this location.
    /// </summary>
    /// <param name="locationId">The data location aggregate identifier.</param>
    /// <param name="reason">Explanation of why the location record is being removed.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// Removal may occur due to data deletion (GDPR Art. 17 right to erasure), migration completion,
    /// or cache/replica cleanup.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> RemoveLocationAsync(
        Guid locationId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all data location records for a specific entity.
    /// </summary>
    /// <param name="entityId">Business identifier of the entity whose locations should be removed.</param>
    /// <param name="reason">Explanation of why the locations are being removed.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the number of locations removed.</returns>
    /// <remarks>
    /// Typically used to support GDPR Article 17 right to erasure — when an entity's data is deleted,
    /// all associated location records should be removed to reflect the current state.
    /// </remarks>
    ValueTask<Either<EncinaError, int>> RemoveByEntityAsync(
        string entityId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a sovereignty violation detected on a data location.
    /// </summary>
    /// <param name="locationId">The data location aggregate identifier.</param>
    /// <param name="dataCategory">Category of personal data involved in the violation.</param>
    /// <param name="violatingRegionCode">Region code that violates the residency policy.</param>
    /// <param name="details">Human-readable details about the violation.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// Per GDPR Article 33, certain violations may trigger breach notification obligations to the
    /// supervisory authority within 72 hours.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> DetectViolationAsync(
        Guid locationId,
        string dataCategory,
        string violatingRegionCode,
        string details,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a previously detected sovereignty violation on a data location.
    /// </summary>
    /// <param name="locationId">The data location aggregate identifier.</param>
    /// <param name="resolution">Description of how the violation was resolved.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> ResolveViolationAsync(
        Guid locationId,
        string resolution,
        CancellationToken cancellationToken = default);

    // ========================================================================
    // Query operations (read-side via DataLocationReadModel)
    // ========================================================================

    /// <summary>
    /// Retrieves a data location by its aggregate identifier.
    /// </summary>
    /// <param name="locationId">The data location aggregate identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error (including not-found) or the location read model.</returns>
    ValueTask<Either<EncinaError, DataLocationReadModel>> GetLocationAsync(
        Guid locationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all data locations for a specific entity.
    /// </summary>
    /// <param name="entityId">Business identifier of the entity.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only list of location read models for the entity.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<DataLocationReadModel>>> GetByEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all data locations in a specific region.
    /// </summary>
    /// <param name="regionCode">The region code to filter by.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only list of location read models in the region.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<DataLocationReadModel>>> GetByRegionAsync(
        string regionCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all data locations for a specific data category.
    /// </summary>
    /// <param name="dataCategory">The data category to filter by.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only list of location read models for the category.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<DataLocationReadModel>>> GetByCategoryAsync(
        string dataCategory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all data locations with active sovereignty violations.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only list of location read models that have active violations.</returns>
    /// <remarks>
    /// Active violations indicate data stored in regions that violate the applicable residency policy.
    /// Per GDPR Article 33, organizations should regularly check for and remediate such violations.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<DataLocationReadModel>>> GetViolationsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the full event history for a data location aggregate.
    /// </summary>
    /// <param name="locationId">The data location aggregate identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// Either an error or the list of domain events that have been applied to this location,
    /// ordered chronologically. Provides a complete data movement history for GDPR Article 5(2)
    /// accountability and Article 58 supervisory authority inquiries.
    /// </returns>
    ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetLocationHistoryAsync(
        Guid locationId,
        CancellationToken cancellationToken = default);
}
