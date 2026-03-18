using Encina.Compliance.DataResidency.Model;

// Event-sourced events implement INotification so they are automatically published
// by Encina.Marten's EventPublishingPipelineBehavior after successful command execution.
// This allows handlers to react to aggregate state changes without a separate notification layer.

namespace Encina.Compliance.DataResidency.Events;

/// <summary>
/// Raised when a new data location is registered, recording where an entity's data is stored.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 30, controllers must maintain records of processing activities including
/// the categories of data and geographic locations where processing occurs. This event captures
/// the initial registration of a data entity's physical storage location.
/// </para>
/// <para>
/// The <paramref name="StorageType"/> distinguishes between primary storage, replicas, caches,
/// backups, and archives — each of which may have different data sovereignty implications
/// under GDPR Recital 101.
/// </para>
/// </remarks>
/// <param name="LocationId">Unique identifier for this data location aggregate.</param>
/// <param name="EntityId">Business identifier of the entity whose data is stored (e.g., customer ID, order ID).</param>
/// <param name="DataCategory">Category of personal data stored (e.g., "personal-data", "healthcare-data").</param>
/// <param name="RegionCode">Region code where the data is stored (ISO 3166-1 alpha-2 or custom identifier).</param>
/// <param name="StorageType">Classification of how the data is stored (Primary, Replica, Cache, Backup, Archive).</param>
/// <param name="StoredAtUtc">Timestamp when the data was stored in this location (UTC).</param>
/// <param name="Metadata">Optional key-value metadata about the storage location.</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record DataLocationRegistered(
    Guid LocationId,
    string EntityId,
    string DataCategory,
    string RegionCode,
    StorageType StorageType,
    DateTimeOffset StoredAtUtc,
    IReadOnlyDictionary<string, string>? Metadata,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when data is migrated from one region to another.
/// </summary>
/// <remarks>
/// <para>
/// Data migration between regions is a cross-border transfer under GDPR Chapter V and requires
/// a valid legal basis (adequacy decision under Art. 45, appropriate safeguards under Art. 46,
/// or derogations under Art. 49).
/// </para>
/// <para>
/// This event captures the previous and new region to maintain a complete data movement
/// history for GDPR Article 5(2) accountability and Article 58 supervisory authority inquiries.
/// </para>
/// </remarks>
/// <param name="LocationId">The data location aggregate identifier.</param>
/// <param name="EntityId">Business identifier of the entity whose data was migrated.</param>
/// <param name="PreviousRegionCode">Region code where the data was previously stored.</param>
/// <param name="NewRegionCode">Region code where the data has been migrated to.</param>
/// <param name="Reason">Explanation of why the data was migrated.</param>
public sealed record DataLocationMigrated(
    Guid LocationId,
    string EntityId,
    string PreviousRegionCode,
    string NewRegionCode,
    string Reason) : INotification;

/// <summary>
/// Raised when a data location is periodically verified to confirm the data remains
/// in the expected region.
/// </summary>
/// <remarks>
/// Regular verification helps organizations demonstrate ongoing compliance with data residency
/// requirements under GDPR Article 5(2) accountability. Verification may be automated
/// (e.g., checking cloud provider metadata) or manual (e.g., audit processes).
/// </remarks>
/// <param name="LocationId">The data location aggregate identifier.</param>
/// <param name="VerifiedAtUtc">Timestamp when the verification was performed (UTC).</param>
public sealed record DataLocationVerified(
    Guid LocationId,
    DateTimeOffset VerifiedAtUtc) : INotification;

/// <summary>
/// Raised when a data location record is removed, indicating the data is no longer stored in this location.
/// </summary>
/// <remarks>
/// Removal may occur due to data deletion (GDPR Art. 17 right to erasure), migration completion,
/// or cache/replica cleanup. The event stream preserves the full location history even after removal.
/// </remarks>
/// <param name="LocationId">The data location aggregate identifier.</param>
/// <param name="EntityId">Business identifier of the entity whose location record is removed.</param>
/// <param name="Reason">Explanation of why the location record was removed.</param>
public sealed record DataLocationRemoved(
    Guid LocationId,
    string EntityId,
    string Reason) : INotification;

/// <summary>
/// Raised when a sovereignty violation is detected — data is found in a region that violates
/// the applicable residency policy.
/// </summary>
/// <remarks>
/// <para>
/// A sovereignty violation occurs when data of a specific <paramref name="DataCategory"/> is stored
/// in a region not permitted by the applicable <see cref="ResidencyPolicyCreated">residency policy</see>.
/// This may happen due to misconfigured replication, unauthorized data movement, or policy changes
/// that retroactively make existing locations non-compliant.
/// </para>
/// <para>
/// Per GDPR Article 33, certain violations may trigger breach notification obligations to the
/// supervisory authority within 72 hours. The organization must assess whether the violation
/// constitutes a personal data breach.
/// </para>
/// </remarks>
/// <param name="LocationId">The data location aggregate identifier where the violation was detected.</param>
/// <param name="EntityId">Business identifier of the affected entity.</param>
/// <param name="DataCategory">Category of personal data involved in the violation.</param>
/// <param name="ViolatingRegionCode">Region code that violates the residency policy.</param>
/// <param name="Details">Human-readable details about the violation.</param>
public sealed record SovereigntyViolationDetected(
    Guid LocationId,
    string EntityId,
    string DataCategory,
    string ViolatingRegionCode,
    string Details) : INotification;

/// <summary>
/// Raised when a previously detected sovereignty violation is resolved.
/// </summary>
/// <remarks>
/// Resolution typically involves migrating the data to an allowed region, updating the residency
/// policy to permit the current region, or removing the data from the violating location.
/// The event stream provides a complete audit trail of violation detection and resolution
/// for GDPR Article 5(2) accountability.
/// </remarks>
/// <param name="LocationId">The data location aggregate identifier where the violation was resolved.</param>
/// <param name="EntityId">Business identifier of the affected entity.</param>
/// <param name="Resolution">Description of how the violation was resolved.</param>
public sealed record SovereigntyViolationResolved(
    Guid LocationId,
    string EntityId,
    string Resolution) : INotification;
