using System.Text.Json;

using Encina.Compliance.DataResidency.Model;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Maps between <see cref="DataLocation"/> domain records and
/// <see cref="DataLocationEntity"/> persistence entities.
/// </summary>
/// <remarks>
/// <para>
/// This mapper handles three key type transformations:
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="DataLocation.Region"/> (<see cref="Region"/>) ↔
/// <see cref="DataLocationEntity.RegionCode"/> (<see cref="string"/>),
/// using <see cref="Region.Code"/> and <see cref="RegionRegistry.GetByCode"/>.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="DataLocation.StorageType"/> (<see cref="StorageType"/>) ↔
/// <see cref="DataLocationEntity.StorageTypeValue"/> (<see cref="int"/>),
/// using integer casting for cross-provider compatibility.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="DataLocation.Metadata"/> (<see cref="IReadOnlyDictionary{TKey, TValue}"/>) ↔
/// <see cref="DataLocationEntity.Metadata"/> (<see cref="string"/>),
/// using JSON serialization for portable storage.
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// Used by store implementations (ADO.NET, Dapper, EF Core, MongoDB) to persist and
/// retrieve data location records without coupling to the domain model.
/// </para>
/// </remarks>
public static class DataLocationMapper
{
    /// <summary>
    /// Converts a domain <see cref="DataLocation"/> to a persistence entity.
    /// </summary>
    /// <param name="location">The domain location to convert.</param>
    /// <returns>A <see cref="DataLocationEntity"/> suitable for persistence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="location"/> is <c>null</c>.</exception>
    public static DataLocationEntity ToEntity(DataLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);

        return new DataLocationEntity
        {
            Id = location.Id,
            EntityId = location.EntityId,
            DataCategory = location.DataCategory,
            RegionCode = location.Region.Code,
            StorageTypeValue = (int)location.StorageType,
            StoredAtUtc = location.StoredAtUtc,
            LastVerifiedAtUtc = location.LastVerifiedAtUtc,
            Metadata = location.Metadata is { Count: > 0 }
                ? JsonSerializer.Serialize(location.Metadata)
                : null
        };
    }

    /// <summary>
    /// Converts a persistence entity back to a domain <see cref="DataLocation"/>.
    /// </summary>
    /// <param name="entity">The persistence entity to convert.</param>
    /// <returns>
    /// A <see cref="DataLocation"/> if the entity state is valid (region code resolves
    /// and enum value is defined), or <c>null</c> if the entity contains invalid values.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    public static DataLocation? ToDomain(DataLocationEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!Enum.IsDefined(typeof(StorageType), entity.StorageTypeValue))
        {
            return null;
        }

        var region = RegionRegistry.GetByCode(entity.RegionCode);
        if (region is null)
        {
            return null;
        }

        IReadOnlyDictionary<string, string>? metadata = null;
        if (!string.IsNullOrEmpty(entity.Metadata))
        {
            metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(entity.Metadata);
        }

        return new DataLocation
        {
            Id = entity.Id,
            EntityId = entity.EntityId,
            DataCategory = entity.DataCategory,
            Region = region,
            StorageType = (StorageType)entity.StorageTypeValue,
            StoredAtUtc = entity.StoredAtUtc,
            LastVerifiedAtUtc = entity.LastVerifiedAtUtc,
            Metadata = metadata
        };
    }
}
