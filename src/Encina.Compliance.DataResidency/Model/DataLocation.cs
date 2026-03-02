namespace Encina.Compliance.DataResidency.Model;

/// <summary>
/// Tracks the physical location of a data entity for residency compliance.
/// </summary>
/// <remarks>
/// <para>
/// A data location record captures where a specific piece of data is stored,
/// what type of storage it uses (primary, replica, cache, backup, archive),
/// and when it was last verified. This enables compliance audits to answer
/// questions like "where is customer X's data stored?" or "what data do we
/// have in region Y?".
/// </para>
/// <para>
/// Location tracking is essential for GDPR Article 30 (records of processing
/// activities) and for responding to supervisory authority inquiries about
/// data processing locations (Article 58).
/// </para>
/// <para>
/// The pipeline behavior automatically records data locations after successful
/// processing when location tracking is enabled in <c>DataResidencyOptions</c>.
/// Manual recording is also supported via <c>IDataLocationStore</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var location = DataLocation.Create(
///     entityId: "customer-42",
///     dataCategory: "personal-data",
///     region: RegionRegistry.DE,
///     storageType: StorageType.Primary);
/// </code>
/// </example>
public sealed record DataLocation
{
    /// <summary>
    /// Unique identifier for this data location record.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Identifier of the data entity being tracked.
    /// </summary>
    /// <remarks>
    /// This is the business identifier of the entity (e.g., customer ID, order ID),
    /// not the database primary key. The same entity may have multiple location
    /// records if it is replicated across regions.
    /// </remarks>
    public required string EntityId { get; init; }

    /// <summary>
    /// The data category of the tracked entity.
    /// </summary>
    /// <remarks>
    /// Examples: "personal-data", "financial-records", "healthcare-data",
    /// "marketing-consent". Must match the categories used in residency policies.
    /// </remarks>
    public required string DataCategory { get; init; }

    /// <summary>
    /// The geographic region where the data is stored.
    /// </summary>
    public required Region Region { get; init; }

    /// <summary>
    /// The type of storage at this location.
    /// </summary>
    /// <remarks>
    /// Distinguishes between the authoritative primary copy and secondary copies
    /// (replicas, caches, backups, archives). All copies must comply with
    /// residency policies — protection follows the data regardless of storage type.
    /// </remarks>
    public required StorageType StorageType { get; init; }

    /// <summary>
    /// Timestamp when the data was stored at this location (UTC).
    /// </summary>
    public required DateTimeOffset StoredAtUtc { get; init; }

    /// <summary>
    /// Timestamp when this location record was last verified (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> if the location has never been verified after initial recording.
    /// Periodic verification ensures that location records remain accurate as
    /// data may be migrated or replicated to different regions.
    /// </remarks>
    public DateTimeOffset? LastVerifiedAtUtc { get; init; }

    /// <summary>
    /// Optional metadata associated with this location record.
    /// </summary>
    /// <remarks>
    /// Can contain additional context such as cloud provider, data center ID,
    /// encryption status, or compliance notes. Example keys: "provider" = "Azure",
    /// "datacenter" = "westeurope", "encrypted" = "true".
    /// </remarks>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Creates a new data location record with a generated unique identifier
    /// and the current UTC timestamp.
    /// </summary>
    /// <param name="entityId">Identifier of the data entity being tracked.</param>
    /// <param name="dataCategory">The data category of the tracked entity.</param>
    /// <param name="region">The geographic region where the data is stored.</param>
    /// <param name="storageType">The type of storage at this location.</param>
    /// <param name="metadata">Optional metadata for the location record.</param>
    /// <returns>A new <see cref="DataLocation"/> with a generated GUID identifier.</returns>
    public static DataLocation Create(
        string entityId,
        string dataCategory,
        Region region,
        StorageType storageType = StorageType.Primary,
        IReadOnlyDictionary<string, string>? metadata = null) =>
        new()
        {
            Id = Guid.NewGuid().ToString("N"),
            EntityId = entityId,
            DataCategory = dataCategory,
            Region = region,
            StorageType = storageType,
            StoredAtUtc = DateTimeOffset.UtcNow,
            Metadata = metadata
        };
}
