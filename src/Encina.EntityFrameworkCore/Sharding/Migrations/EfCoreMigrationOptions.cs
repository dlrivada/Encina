using Encina.Sharding.Migrations;

namespace Encina.EntityFrameworkCore.Sharding.Migrations;

/// <summary>
/// Configuration options specific to EF Core shard migration coordination.
/// </summary>
/// <remarks>
/// <para>
/// These options extend the base <see cref="MigrationCoordinationOptions"/> with
/// EF Core-specific settings such as whether to use <c>Database.Migrate()</c> for
/// applying EF Core migrations or custom SQL scripts.
/// </para>
/// <para>
/// Configured via the <c>WithEfCoreMigrations</c> builder method on the
/// migration coordination builder.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaEFCoreShardMigration&lt;AppDbContext&gt;(options =>
/// {
///     options.UseEfCoreMigrate = true;
///     options.HistoryTableName = "__ShardMigrationHistory";
///     options.HistoryTableSchema = "encina";
/// });
/// </code>
/// </example>
public sealed class EfCoreMigrationOptions
{
    /// <summary>
    /// Gets or sets whether to use EF Core's built-in <c>Database.Migrate()</c> method
    /// for applying pending migrations to each shard.
    /// </summary>
    /// <value>Defaults to <see langword="false"/>.</value>
    /// <remarks>
    /// <para>
    /// When <see langword="true"/>, the <see cref="EfCoreMigrationExecutor"/> invokes
    /// <c>Database.Migrate()</c> on each shard's DbContext, applying all pending EF Core
    /// migrations. When <see langword="false"/>, the coordinator uses the raw SQL-based
    /// <see cref="MigrationScript"/> approach for explicit control over DDL.
    /// </para>
    /// </remarks>
    public bool UseEfCoreMigrate { get; set; }

    /// <summary>
    /// Gets or sets the name of the migration history table used by
    /// <see cref="EfCoreMigrationHistoryStore"/> to track applied migrations per shard.
    /// </summary>
    /// <value>Defaults to <c>"__ShardMigrationHistory"</c>.</value>
    public string HistoryTableName { get; set; } = "__ShardMigrationHistory";

    /// <summary>
    /// Gets or sets the schema for the migration history table.
    /// When <see langword="null"/>, the provider's default schema is used.
    /// </summary>
    /// <value>Defaults to <see langword="null"/>.</value>
    /// <remarks>
    /// <para>
    /// For SQL Server this defaults to <c>dbo</c>, for PostgreSQL to <c>public</c>,
    /// and for MySQL/SQLite this is ignored.
    /// </para>
    /// </remarks>
    public string? HistoryTableSchema { get; set; }

    /// <summary>
    /// Gets or sets whether to automatically create the migration history table
    /// if it does not exist when applying or querying migrations.
    /// </summary>
    /// <value>Defaults to <see langword="true"/>.</value>
    public bool AutoCreateHistoryTable { get; set; } = true;

    /// <summary>
    /// Gets or sets the command timeout in seconds for migration DDL statements
    /// executed through EF Core's <c>Database.ExecuteSqlRaw</c>.
    /// </summary>
    /// <value>Defaults to <c>300</c> (5 minutes).</value>
    public int CommandTimeoutSeconds { get; set; } = 300;
}
