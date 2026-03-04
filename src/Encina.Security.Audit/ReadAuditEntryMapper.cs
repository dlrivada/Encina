using System.Text.Json;

namespace Encina.Security.Audit;

/// <summary>
/// Provides bidirectional mapping between <see cref="ReadAuditEntry"/> domain records
/// and <see cref="ReadAuditEntryEntity"/> persistence entities.
/// </summary>
/// <remarks>
/// <para>
/// This mapper is shared across all persistence providers (EF Core, ADO.NET, Dapper)
/// to ensure consistent serialization of enum values and metadata JSON.
/// </para>
/// <para>
/// The <see cref="ReadAccessMethod"/> enum is stored as its integer ordinal value
/// in the <see cref="ReadAuditEntryEntity.AccessMethod"/> property.
/// The <see cref="ReadAuditEntry.Metadata"/> dictionary is serialized to/from JSON.
/// </para>
/// </remarks>
public static class ReadAuditEntryMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Maps a <see cref="ReadAuditEntry"/> domain record to a <see cref="ReadAuditEntryEntity"/>.
    /// </summary>
    /// <param name="entry">The domain record to map.</param>
    /// <returns>A persistence entity ready for storage.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is null.</exception>
    public static ReadAuditEntryEntity MapToEntity(ReadAuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new ReadAuditEntryEntity
        {
            Id = entry.Id,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            UserId = entry.UserId,
            TenantId = entry.TenantId,
            AccessedAtUtc = entry.AccessedAtUtc,
            CorrelationId = entry.CorrelationId,
            Purpose = entry.Purpose,
            AccessMethod = (int)entry.AccessMethod,
            EntityCount = entry.EntityCount,
            Metadata = SerializeMetadata(entry.Metadata)
        };
    }

    /// <summary>
    /// Maps a <see cref="ReadAuditEntryEntity"/> persistence entity to a <see cref="ReadAuditEntry"/> domain record.
    /// </summary>
    /// <param name="entity">The persistence entity to map.</param>
    /// <returns>An immutable domain record.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is null.</exception>
    public static ReadAuditEntry MapToRecord(ReadAuditEntryEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ReadAuditEntry
        {
            Id = entity.Id,
            EntityType = entity.EntityType,
            EntityId = entity.EntityId,
            UserId = entity.UserId,
            TenantId = entity.TenantId,
            AccessedAtUtc = entity.AccessedAtUtc,
            CorrelationId = entity.CorrelationId,
            Purpose = entity.Purpose,
            AccessMethod = (ReadAccessMethod)entity.AccessMethod,
            EntityCount = entity.EntityCount,
            Metadata = DeserializeMetadata(entity.Metadata)
        };
    }

    /// <summary>
    /// Serializes a metadata dictionary to a JSON string.
    /// </summary>
    /// <param name="metadata">The metadata dictionary to serialize.</param>
    /// <returns>A JSON string, or <c>null</c> if the dictionary is empty.</returns>
    public static string? SerializeMetadata(IReadOnlyDictionary<string, object?> metadata)
    {
        if (metadata.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(metadata, JsonOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to a metadata dictionary.
    /// </summary>
    /// <param name="json">The JSON string to deserialize, or <c>null</c>.</param>
    /// <returns>A dictionary of metadata, or an empty dictionary if the input is null/empty/invalid.</returns>
    public static Dictionary<string, object?> DeserializeMetadata(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return new Dictionary<string, object?>();
        }

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json, JsonOptions);
            return dict ?? new Dictionary<string, object?>();
        }
        catch
        {
            return new Dictionary<string, object?>();
        }
    }
}
