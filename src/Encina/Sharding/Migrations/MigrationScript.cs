namespace Encina.Sharding.Migrations;

/// <summary>
/// An immutable migration script containing the forward (up) and reverse (down) DDL
/// statements to apply and rollback a schema change across shards.
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="MigrationScript"/> is identified by a unique <see cref="Id"/> and carries
/// a <see cref="Checksum"/> that the coordinator uses to verify integrity before applying
/// the script to any shard. Scripts are intended to be provider-agnostic at the model level;
/// provider-specific SQL is the caller's responsibility.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var script = new MigrationScript(
///     Id: "20260216_add_orders_index",
///     UpSql: "CREATE INDEX idx_orders_status ON orders (status);",
///     DownSql: "DROP INDEX idx_orders_status;",
///     Description: "Add index on orders.status for faster filtering",
///     Checksum: "sha256:a1b2c3d4...");
///
/// // Pass to the coordinator
/// var result = await coordinator.ApplyToAllShardsAsync(script, options, ct);
/// result.Match(
///     Right: r => logger.LogInformation("Migration applied to {Count} shards", r.PerShardStatus.Count),
///     Left: error => logger.LogError("Migration failed: {Error}", error.Message));
/// </code>
/// </example>
/// <param name="Id">
/// Unique identifier for the migration (e.g., <c>"20260216_add_orders_index"</c>).
/// Used for idempotency checks and migration history tracking.
/// </param>
/// <param name="UpSql">The forward DDL statement(s) that apply the schema change.</param>
/// <param name="DownSql">The reverse DDL statement(s) that rollback the schema change.</param>
/// <param name="Description">A human-readable description of what this migration does.</param>
/// <param name="Checksum">
/// Integrity checksum for the script content (e.g., <c>"sha256:a1b2c3..."</c>).
/// The coordinator validates this before applying to each shard.
/// </param>
public sealed record MigrationScript(
    string Id,
    string UpSql,
    string DownSql,
    string Description,
    string Checksum)
{
    /// <summary>Gets the unique migration identifier.</summary>
    public string Id { get; } = !string.IsNullOrWhiteSpace(Id)
        ? Id
        : throw new ArgumentException("Migration script ID cannot be null or whitespace.", nameof(Id));

    /// <summary>Gets the forward DDL statement(s).</summary>
    public string UpSql { get; } = !string.IsNullOrWhiteSpace(UpSql)
        ? UpSql
        : throw new ArgumentException("UpSql cannot be null or whitespace.", nameof(UpSql));

    /// <summary>Gets the reverse DDL statement(s).</summary>
    public string DownSql { get; } = !string.IsNullOrWhiteSpace(DownSql)
        ? DownSql
        : throw new ArgumentException("DownSql cannot be null or whitespace.", nameof(DownSql));

    /// <summary>Gets the human-readable description.</summary>
    public string Description { get; } = !string.IsNullOrWhiteSpace(Description)
        ? Description
        : throw new ArgumentException("Description cannot be null or whitespace.", nameof(Description));

    /// <summary>Gets the integrity checksum.</summary>
    public string Checksum { get; } = !string.IsNullOrWhiteSpace(Checksum)
        ? Checksum
        : throw new ArgumentException("Checksum cannot be null or whitespace.", nameof(Checksum));
}
