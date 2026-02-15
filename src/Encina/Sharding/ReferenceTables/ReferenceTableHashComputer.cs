using System.IO.Hashing;
using System.Text.Json;

namespace Encina.Sharding.ReferenceTables;

/// <summary>
/// Computes deterministic content hashes for reference table data, used by
/// <see cref="PollingRefreshDetector"/> to detect changes between polling cycles.
/// </summary>
/// <remarks>
/// <para>
/// The hash algorithm:
/// <list type="number">
/// <item>Sort entities by primary key value for deterministic ordering.</item>
/// <item>Serialize each entity to UTF-8 JSON via <see cref="JsonSerializer"/>.</item>
/// <item>Feed all bytes into <see cref="XxHash64"/> (non-cryptographic, fast).</item>
/// <item>Return the 64-bit hash as a 16-character hexadecimal string.</item>
/// </list>
/// </para>
/// <para>
/// This shared implementation ensures all provider-specific
/// <see cref="IReferenceTableStore.GetHashAsync{TEntity}"/> methods produce
/// consistent hashes for the same data, regardless of the underlying database.
/// </para>
/// </remarks>
public static class ReferenceTableHashComputer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false,
    };

    /// <summary>
    /// Computes a deterministic content hash for the given entities.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entities">The entities to hash. May be empty.</param>
    /// <returns>A 16-character hexadecimal hash string.</returns>
    public static string ComputeHash<TEntity>(IReadOnlyList<TEntity> entities)
        where TEntity : class
    {
        if (entities.Count == 0)
        {
            return "0000000000000000";
        }

        var metadata = EntityMetadataCache.GetOrCreate<TEntity>();
        var pkProperty = metadata.PrimaryKey.Property;

        // Sort by primary key value for deterministic ordering
        var sorted = entities
            .OrderBy(e => pkProperty.GetValue(e) as IComparable)
            .ToList();

        var hash = new XxHash64();

        foreach (var entity in sorted)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(entity, SerializerOptions);
            hash.Append(bytes);
        }

        return hash.GetCurrentHashAsUInt64().ToString("x16", System.Globalization.CultureInfo.InvariantCulture);
    }
}
