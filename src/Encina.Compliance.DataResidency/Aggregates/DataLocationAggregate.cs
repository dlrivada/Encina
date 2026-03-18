using Encina.Compliance.DataResidency.Events;
using Encina.Compliance.DataResidency.Model;
using Encina.DomainModeling;

namespace Encina.Compliance.DataResidency.Aggregates;

/// <summary>
/// Event-sourced aggregate representing a data entity's physical storage location for
/// data residency and sovereignty enforcement.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 30, controllers must maintain records of processing activities including
/// the categories of data and geographic locations where processing occurs. This aggregate
/// tracks where a specific entity's data is stored, supports migration between regions,
/// periodic verification, and sovereignty violation detection and resolution.
/// </para>
/// <para>
/// The lifecycle is: <c>Registered → Active</c> (with optional <c>Migrated</c>, <c>Verified</c>,
/// <c>ViolationDetected</c>, <c>ViolationResolved</c> transitions) → <c>Removed</c>.
/// A removed location cannot be operated on further.
/// </para>
/// <para>
/// Sovereignty violations are tracked on the location aggregate because violations are inherently
/// about a specific entity's data being found in a non-compliant region. The violation lifecycle
/// (<c>Detected → Resolved</c>) provides a complete audit trail for GDPR Article 5(2) accountability
/// and Article 33 breach notification assessment.
/// </para>
/// <para>
/// All state changes are captured as immutable events, providing a complete data movement history
/// for GDPR Article 5(2) accountability and Article 58 supervisory authority inquiries.
/// </para>
/// </remarks>
public sealed class DataLocationAggregate : AggregateBase
{
    /// <summary>
    /// Business identifier of the entity whose data is stored (e.g., customer ID, order ID).
    /// </summary>
    public string EntityId { get; private set; } = string.Empty;

    /// <summary>
    /// Category of personal data stored (e.g., "personal-data", "healthcare-data").
    /// </summary>
    public string DataCategory { get; private set; } = string.Empty;

    /// <summary>
    /// Region code where the data is currently stored.
    /// </summary>
    /// <remarks>
    /// ISO 3166-1 alpha-2 country code, regional code (e.g., "EU"), or custom identifier
    /// (e.g., "AZURE-WESTEU"). Updated when data is migrated via <see cref="Migrate"/>.
    /// </remarks>
    public string RegionCode { get; private set; } = string.Empty;

    /// <summary>
    /// Classification of how the data is stored (Primary, Replica, Cache, Backup, Archive).
    /// </summary>
    public StorageType StorageType { get; private set; }

    /// <summary>
    /// Timestamp when the data was first stored in this location (UTC).
    /// </summary>
    public DateTimeOffset StoredAtUtc { get; private set; }

    /// <summary>
    /// Timestamp of the most recent location verification (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> if the location has never been verified after initial registration.
    /// </remarks>
    public DateTimeOffset? LastVerifiedAtUtc { get; private set; }

    /// <summary>
    /// Optional key-value metadata about the storage location.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; private set; }

    /// <summary>
    /// Whether this location record has been removed (data no longer stored here).
    /// </summary>
    public bool IsRemoved { get; private set; }

    /// <summary>
    /// Whether a sovereignty violation is currently active on this location.
    /// </summary>
    /// <remarks>
    /// <see langword="true"/> when a violation has been detected but not yet resolved.
    /// </remarks>
    public bool HasViolation { get; private set; }

    /// <summary>
    /// Details of the current sovereignty violation, if any.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when no violation is active (<see cref="HasViolation"/> is <see langword="false"/>).
    /// </remarks>
    public string? ViolationDetails { get; private set; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; private set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; private set; }

    /// <summary>
    /// Registers a new data location, recording where an entity's data is stored.
    /// </summary>
    /// <param name="id">Unique identifier for the new data location aggregate.</param>
    /// <param name="entityId">Business identifier of the entity whose data is stored.</param>
    /// <param name="dataCategory">Category of personal data stored.</param>
    /// <param name="regionCode">Region code where the data is stored.</param>
    /// <param name="storageType">Classification of how the data is stored.</param>
    /// <param name="storedAtUtc">Timestamp when the data was stored (UTC).</param>
    /// <param name="metadata">Optional key-value metadata about the storage location.</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy scoping.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith scoping.</param>
    /// <returns>A new <see cref="DataLocationAggregate"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="entityId"/>, <paramref name="dataCategory"/>, or <paramref name="regionCode"/> is null or whitespace.</exception>
    public static DataLocationAggregate Register(
        Guid id,
        string entityId,
        string dataCategory,
        string regionCode,
        StorageType storageType,
        DateTimeOffset storedAtUtc,
        IReadOnlyDictionary<string, string>? metadata = null,
        string? tenantId = null,
        string? moduleId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);
        ArgumentException.ThrowIfNullOrWhiteSpace(regionCode);

        var aggregate = new DataLocationAggregate();
        aggregate.RaiseEvent(new DataLocationRegistered(
            id, entityId, dataCategory, regionCode, storageType,
            storedAtUtc, metadata, tenantId, moduleId));
        return aggregate;
    }

    /// <summary>
    /// Migrates data from the current region to a new region.
    /// </summary>
    /// <remarks>
    /// Data migration between regions is a cross-border transfer under GDPR Chapter V.
    /// The caller is responsible for ensuring a valid legal basis exists before migration.
    /// </remarks>
    /// <param name="newRegionCode">Region code where the data is being migrated to.</param>
    /// <param name="reason">Explanation of why the data is being migrated.</param>
    /// <exception cref="InvalidOperationException">Thrown when the location has been removed.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="newRegionCode"/> or <paramref name="reason"/> is null or whitespace.</exception>
    public void Migrate(string newRegionCode, string reason)
    {
        if (IsRemoved)
        {
            throw new InvalidOperationException(
                $"Cannot migrate location '{Id}' because it has been removed.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(newRegionCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        RaiseEvent(new DataLocationMigrated(Id, EntityId, RegionCode, newRegionCode, reason));
    }

    /// <summary>
    /// Records a periodic verification that the data remains in the expected region.
    /// </summary>
    /// <param name="verifiedAtUtc">Timestamp when the verification was performed (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the location has been removed.</exception>
    public void Verify(DateTimeOffset verifiedAtUtc)
    {
        if (IsRemoved)
        {
            throw new InvalidOperationException(
                $"Cannot verify location '{Id}' because it has been removed.");
        }

        RaiseEvent(new DataLocationVerified(Id, verifiedAtUtc));
    }

    /// <summary>
    /// Removes the location record, indicating the data is no longer stored in this location.
    /// </summary>
    /// <param name="reason">Explanation of why the location record is being removed.</param>
    /// <exception cref="InvalidOperationException">Thrown when the location is already removed.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="reason"/> is null or whitespace.</exception>
    public void Remove(string reason)
    {
        if (IsRemoved)
        {
            throw new InvalidOperationException(
                $"Cannot remove location '{Id}' because it is already removed.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        RaiseEvent(new DataLocationRemoved(Id, EntityId, reason));
    }

    /// <summary>
    /// Detects a sovereignty violation on this data location.
    /// </summary>
    /// <remarks>
    /// A violation occurs when the data is stored in a region that does not comply with
    /// the applicable residency policy. Per GDPR Article 33, certain violations may trigger
    /// breach notification obligations.
    /// </remarks>
    /// <param name="dataCategory">Category of personal data involved in the violation.</param>
    /// <param name="violatingRegionCode">Region code that violates the residency policy.</param>
    /// <param name="details">Human-readable details about the violation.</param>
    /// <exception cref="InvalidOperationException">Thrown when the location has been removed or already has an active violation.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="dataCategory"/>, <paramref name="violatingRegionCode"/>, or <paramref name="details"/> is null or whitespace.</exception>
    public void DetectViolation(string dataCategory, string violatingRegionCode, string details)
    {
        if (IsRemoved)
        {
            throw new InvalidOperationException(
                $"Cannot detect violation on location '{Id}' because it has been removed.");
        }

        if (HasViolation)
        {
            throw new InvalidOperationException(
                $"Cannot detect violation on location '{Id}' because it already has an active violation. Resolve the existing violation first.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);
        ArgumentException.ThrowIfNullOrWhiteSpace(violatingRegionCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(details);

        RaiseEvent(new SovereigntyViolationDetected(Id, EntityId, dataCategory, violatingRegionCode, details));
    }

    /// <summary>
    /// Resolves a previously detected sovereignty violation on this data location.
    /// </summary>
    /// <remarks>
    /// Resolution typically involves migrating data to an allowed region, updating the residency
    /// policy, or removing data from the violating location.
    /// </remarks>
    /// <param name="resolution">Description of how the violation was resolved.</param>
    /// <exception cref="InvalidOperationException">Thrown when no active violation exists to resolve.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="resolution"/> is null or whitespace.</exception>
    public void ResolveViolation(string resolution)
    {
        if (!HasViolation)
        {
            throw new InvalidOperationException(
                $"Cannot resolve violation on location '{Id}' because no active violation exists.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(resolution);

        RaiseEvent(new SovereigntyViolationResolved(Id, EntityId, resolution));
    }

    /// <inheritdoc />
    protected override void Apply(object domainEvent)
    {
        switch (domainEvent)
        {
            case DataLocationRegistered e:
                Id = e.LocationId;
                EntityId = e.EntityId;
                DataCategory = e.DataCategory;
                RegionCode = e.RegionCode;
                StorageType = e.StorageType;
                StoredAtUtc = e.StoredAtUtc;
                Metadata = e.Metadata;
                TenantId = e.TenantId;
                ModuleId = e.ModuleId;
                break;

            case DataLocationMigrated e:
                RegionCode = e.NewRegionCode;
                break;

            case DataLocationVerified e:
                LastVerifiedAtUtc = e.VerifiedAtUtc;
                break;

            case DataLocationRemoved:
                IsRemoved = true;
                break;

            case SovereigntyViolationDetected e:
                HasViolation = true;
                ViolationDetails = e.Details;
                break;

            case SovereigntyViolationResolved:
                HasViolation = false;
                ViolationDetails = null;
                break;
        }
    }
}
