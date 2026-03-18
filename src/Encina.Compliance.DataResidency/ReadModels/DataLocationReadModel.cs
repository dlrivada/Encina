using Encina.Compliance.DataResidency.Model;
using Encina.Marten.Projections;

namespace Encina.Compliance.DataResidency.ReadModels;

/// <summary>
/// Query-optimized projected view of a data location, built from
/// <see cref="Aggregates.DataLocationAggregate"/> events.
/// </summary>
/// <remarks>
/// <para>
/// This read model is materialized from the data location aggregate event stream by
/// <see cref="DataLocationProjection"/>. It provides an efficient query view without
/// replaying events, while the underlying event stream maintains the full data movement
/// history for GDPR Article 5(2) accountability and Article 58 supervisory authority inquiries.
/// </para>
/// <para>
/// Properties have mutable setters because projections update them incrementally
/// as events are applied. The <see cref="LastModifiedAtUtc"/> timestamp is updated
/// on every event, enabling efficient change detection.
/// </para>
/// <para>
/// Tracks the full location lifecycle (Registered → Active → Removed), including
/// region migrations, periodic verifications, and sovereignty violation detection
/// and resolution per GDPR Chapter V (Articles 44–49).
/// </para>
/// </remarks>
public sealed class DataLocationReadModel : IReadModel
{
    /// <summary>
    /// Unique identifier for this data location (matches the aggregate stream ID).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Business identifier of the entity whose data is stored (e.g., customer ID, order ID).
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Category of personal data stored (e.g., "personal-data", "healthcare-data").
    /// </summary>
    public string DataCategory { get; set; } = string.Empty;

    /// <summary>
    /// Region code where the data is currently stored.
    /// </summary>
    /// <remarks>
    /// ISO 3166-1 alpha-2 country code, regional code (e.g., "EU"), or custom identifier
    /// (e.g., "AZURE-WESTEU"). Updated when data is migrated.
    /// </remarks>
    public string RegionCode { get; set; } = string.Empty;

    /// <summary>
    /// Classification of how the data is stored (Primary, Replica, Cache, Backup, Archive).
    /// </summary>
    public StorageType StorageType { get; set; }

    /// <summary>
    /// Timestamp when the data was first stored in this location (UTC).
    /// </summary>
    public DateTimeOffset StoredAtUtc { get; set; }

    /// <summary>
    /// Timestamp of the most recent location verification (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> if the location has never been verified after initial registration.
    /// </remarks>
    public DateTimeOffset? LastVerifiedAtUtc { get; set; }

    /// <summary>
    /// Optional key-value metadata about the storage location.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Whether this location record has been removed (data no longer stored here).
    /// </summary>
    public bool IsRemoved { get; set; }

    /// <summary>
    /// Whether a sovereignty violation is currently active on this location.
    /// </summary>
    /// <remarks>
    /// <see langword="true"/> when a violation has been detected but not yet resolved.
    /// </remarks>
    public bool HasViolation { get; set; }

    /// <summary>
    /// Details of the current sovereignty violation, if any.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when no violation is active (<see cref="HasViolation"/> is <see langword="false"/>).
    /// </remarks>
    public string? ViolationDetails { get; set; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; set; }

    /// <summary>
    /// The UTC timestamp when this data location was registered.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Timestamp of the last modification to this location record (UTC).
    /// </summary>
    /// <remarks>
    /// Updated on every event applied to the data location aggregate.
    /// Enables efficient change detection and cache invalidation.
    /// </remarks>
    public DateTimeOffset LastModifiedAtUtc { get; set; }

    /// <summary>
    /// Event stream version for optimistic concurrency.
    /// </summary>
    /// <remarks>
    /// Incremented on every event. Matches the aggregate's <see cref="DomainModeling.AggregateBase.Version"/>.
    /// </remarks>
    public int Version { get; set; }
}
