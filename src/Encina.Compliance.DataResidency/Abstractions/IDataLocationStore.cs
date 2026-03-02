using Encina.Compliance.DataResidency.Model;

using LanguageExt;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Store for recording and querying data location records.
/// </summary>
/// <remarks>
/// <para>
/// The data location store tracks where data entities are physically stored and processed,
/// enabling the system to verify compliance with residency policies and to provide evidence
/// of data location for regulatory audits.
/// </para>
/// <para>
/// Per GDPR Article 30(1)(e), the controller must maintain records of processing activities
/// including "where applicable, transfers of personal data to a third country". Data location
/// records provide the foundation for demonstrating that data resides only in approved regions.
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
/// // Record a data location
/// var location = DataLocation.Create(
///     entityId: "customer-42",
///     dataCategory: "personal-data",
///     region: RegionRegistry.Germany,
///     storageType: StorageType.Primary);
///
/// await locationStore.RecordAsync(location, cancellationToken);
///
/// // Query locations for an entity
/// var locations = await locationStore.GetByEntityAsync("customer-42", cancellationToken);
/// </code>
/// </example>
public interface IDataLocationStore
{
    /// <summary>
    /// Records a new data location entry.
    /// </summary>
    /// <param name="location">The data location record to persist.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the location
    /// could not be recorded (e.g., duplicate ID).
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> RecordAsync(
        DataLocation location,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all data location records for a specific entity.
    /// </summary>
    /// <param name="entityId">The identifier of the data entity.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of data locations for the entity, or an <see cref="EncinaError"/>
    /// on failure. Returns an empty list if no locations exist for the entity.
    /// </returns>
    /// <remarks>
    /// An entity may have multiple location records if it is stored across several regions
    /// (e.g., primary in EU, replica in US) or storage types (primary, cache, backup).
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<DataLocation>>> GetByEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all data location records for a specific region.
    /// </summary>
    /// <param name="region">The region to query.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of data locations in the specified region, or an <see cref="EncinaError"/>
    /// on failure. Returns an empty list if no data is stored in the region.
    /// </returns>
    /// <remarks>
    /// Useful for compliance audits to identify all data stored in a particular jurisdiction,
    /// or for data migration planning when a region's adequacy status changes.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<DataLocation>>> GetByRegionAsync(
        Region region,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all data location records for a specific data category.
    /// </summary>
    /// <param name="dataCategory">The data category to query (e.g., "personal-data", "healthcare-data").</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of data locations for the category, or an <see cref="EncinaError"/>
    /// on failure. Returns an empty list if no locations exist for the category.
    /// </returns>
    /// <remarks>
    /// Useful for generating category-specific compliance reports and for verifying
    /// that all instances of a data category comply with their residency policy.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<DataLocation>>> GetByCategoryAsync(
        string dataCategory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all data location records for a specific entity.
    /// </summary>
    /// <param name="entityId">The identifier of the data entity whose locations should be removed.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the deletion failed.
    /// Returns success even if no locations existed for the entity.
    /// </returns>
    /// <remarks>
    /// Typically called after a data subject erasure request (GDPR Art. 17) to remove
    /// location tracking records once the actual data has been deleted.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> DeleteByEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default);
}
