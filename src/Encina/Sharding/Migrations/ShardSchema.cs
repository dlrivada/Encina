namespace Encina.Sharding.Migrations;

/// <summary>
/// A snapshot of a shard's database schema at a point in time.
/// </summary>
/// <remarks>
/// <para>
/// Produced by <see cref="ISchemaIntrospector"/> implementations that query
/// provider-specific system catalogs (<c>INFORMATION_SCHEMA</c>, <c>sqlite_master</c>,
/// <c>pg_catalog</c>, etc.).
/// </para>
/// </remarks>
/// <param name="ShardId">The shard this schema belongs to.</param>
/// <param name="Tables">The tables in this shard's schema.</param>
/// <param name="IntrospectedAtUtc">UTC timestamp when the schema was read.</param>
public sealed record ShardSchema(
    string ShardId,
    IReadOnlyList<TableSchema> Tables,
    DateTimeOffset IntrospectedAtUtc)
{
    /// <summary>Gets the shard identifier.</summary>
    public string ShardId { get; } = !string.IsNullOrWhiteSpace(ShardId)
        ? ShardId
        : throw new ArgumentException("Shard ID cannot be null or whitespace.", nameof(ShardId));
}
