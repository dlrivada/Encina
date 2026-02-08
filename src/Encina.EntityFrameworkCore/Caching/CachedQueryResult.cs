using System.Text.Json.Serialization;

namespace Encina.EntityFrameworkCore.Caching;

/// <summary>
/// Represents a cached query result containing column schema and row data.
/// </summary>
/// <remarks>
/// <para>
/// This model is designed for efficient serialization with System.Text.Json and serves as
/// the storage format for cached EF Core query results. When a cache hit occurs, the
/// <c>QueryCacheInterceptor</c> reconstructs a <see cref="System.Data.Common.DbDataReader"/>
/// from this model to feed data back to EF Core's entity materializer.
/// </para>
/// <para>
/// The column schema (<see cref="Columns"/>) preserves the metadata needed to reconstruct
/// a compatible <see cref="System.Data.Common.DbDataReader"/>, including column names, ordinals,
/// data type names, and nullability.
/// </para>
/// </remarks>
public sealed class CachedQueryResult
{
    /// <summary>
    /// Gets or sets the column schema metadata for the cached result set.
    /// </summary>
    /// <remarks>
    /// Each entry describes a column in the result set, preserving the metadata required
    /// to reconstruct a <see cref="System.Data.Common.DbDataReader"/> from cached data.
    /// The order of columns matches the ordinal positions of the original result set.
    /// </remarks>
    [JsonPropertyName("columns")]
    public required List<CachedColumnSchema> Columns { get; init; }

    /// <summary>
    /// Gets or sets the row data as a list of object arrays.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each element is an array of values corresponding to the columns defined in <see cref="Columns"/>.
    /// Values are stored as their original CLR types for JSON serialization compatibility.
    /// </para>
    /// <para>
    /// <c>DBNull</c> values are represented as <c>null</c> in the serialized form.
    /// </para>
    /// </remarks>
    [JsonPropertyName("rows")]
    public required List<object?[]> Rows { get; init; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this result was cached.
    /// </summary>
    /// <remarks>
    /// Used for diagnostics and cache freshness monitoring. This timestamp reflects
    /// when the query was executed and the result was stored, not when it was retrieved.
    /// </remarks>
    [JsonPropertyName("cachedAtUtc")]
    public required DateTime CachedAtUtc { get; init; }
}

/// <summary>
/// Describes the schema of a single column in a cached query result.
/// </summary>
/// <param name="Name">The column name as returned by the database.</param>
/// <param name="Ordinal">The zero-based ordinal position of the column in the result set.</param>
/// <param name="DataTypeName">
/// The database-specific data type name (e.g., <c>"nvarchar"</c>, <c>"int"</c>, <c>"uuid"</c>).
/// </param>
/// <param name="FieldType">
/// The CLR type name of the column value (e.g., <c>"System.String"</c>, <c>"System.Int32"</c>).
/// Stored as a string for JSON serialization compatibility.
/// </param>
/// <param name="AllowDBNull">
/// <c>true</c> if the column allows <c>NULL</c> values; otherwise, <c>false</c>.
/// </param>
/// <remarks>
/// <para>
/// This record preserves the minimum metadata required to reconstruct a
/// <see cref="System.Data.Common.DbDataReader"/> from cached data. The <paramref name="FieldType"/>
/// is stored as a fully-qualified type name string to ensure JSON round-trip compatibility
/// without requiring type resolution during serialization.
/// </para>
/// </remarks>
[JsonSerializable(typeof(CachedColumnSchema))]
public sealed record CachedColumnSchema(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("ordinal")] int Ordinal,
    [property: JsonPropertyName("dataTypeName")] string DataTypeName,
    [property: JsonPropertyName("fieldType")] string FieldType,
    [property: JsonPropertyName("allowDBNull")] bool AllowDBNull);
