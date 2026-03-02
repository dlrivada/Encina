namespace Encina.Compliance.DataResidency;

/// <summary>
/// Persistence entity for <see cref="Model.DataLocation"/>.
/// </summary>
/// <remarks>
/// <para>
/// This entity provides a database-agnostic representation of a data location record,
/// using primitive types suitable for any storage provider (ADO.NET, Dapper, EF Core, MongoDB).
/// </para>
/// <para>
/// Key type transformations:
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="Model.DataLocation.Region"/> (<see cref="Model.Region"/>) is stored
/// as <see cref="RegionCode"/> (<see cref="string"/>) using the region's ISO code.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Model.DataLocation.StorageType"/> (<see cref="Model.StorageType"/>) is stored
/// as <see cref="StorageTypeValue"/> (<see cref="int"/>) for cross-provider compatibility.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Model.DataLocation.Metadata"/> (<see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/>)
/// is stored as <see cref="Metadata"/> (<see cref="string"/>) in JSON format.
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// Use <see cref="DataLocationMapper"/> to convert between this entity and
/// <see cref="Model.DataLocation"/>.
/// </para>
/// </remarks>
public sealed class DataLocationEntity
{
    /// <summary>
    /// Unique identifier for this data location record.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Identifier of the data entity being tracked.
    /// </summary>
    public required string EntityId { get; set; }

    /// <summary>
    /// The data category of the tracked entity.
    /// </summary>
    public required string DataCategory { get; set; }

    /// <summary>
    /// The ISO region code where the data is stored.
    /// </summary>
    /// <remarks>
    /// Resolved to a <see cref="Model.Region"/> via <see cref="Model.RegionRegistry.GetByCode"/>
    /// during domain mapping.
    /// </remarks>
    public required string RegionCode { get; set; }

    /// <summary>
    /// Integer value of the <see cref="Model.StorageType"/> enum.
    /// </summary>
    /// <remarks>
    /// Values: Primary=0, Replica=1, Cache=2, Backup=3, Archive=4.
    /// </remarks>
    public required int StorageTypeValue { get; set; }

    /// <summary>
    /// Timestamp when the data was stored at this location (UTC).
    /// </summary>
    public DateTimeOffset StoredAtUtc { get; set; }

    /// <summary>
    /// Timestamp when this location record was last verified (UTC).
    /// </summary>
    public DateTimeOffset? LastVerifiedAtUtc { get; set; }

    /// <summary>
    /// Optional metadata stored as a JSON string.
    /// </summary>
    /// <remarks>
    /// Serialized representation of the key-value metadata dictionary.
    /// <c>null</c> when no metadata is associated with the location.
    /// </remarks>
    public string? Metadata { get; set; }
}
