namespace Encina.Messaging.Temporal;

/// <summary>
/// Configuration options for SQL Server temporal table support.
/// </summary>
/// <remarks>
/// <para>
/// These options control how temporal tables are configured and queried across
/// all data access providers that support temporal tables.
/// </para>
/// <para>
/// <b>Database Support</b>:
/// <list type="bullet">
/// <item><description><b>SQL Server</b>: Full support via system-versioned temporal tables (SQL Server 2016+)</description></item>
/// <item><description><b>PostgreSQL</b>: Limited support via temporal_tables extension (not currently implemented)</description></item>
/// <item><description><b>MySQL/SQLite</b>: Not supported natively</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Design Philosophy</b>: All options have sensible defaults. The defaults follow
/// SQL Server temporal table conventions for column naming and history table organization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
/// {
///     config.UseTemporalTables = true;
///     config.TemporalTableOptions.DefaultHistoryTableSuffix = "History";
///     config.TemporalTableOptions.DefaultPeriodStartColumnName = "ValidFrom";
///     config.TemporalTableOptions.DefaultPeriodEndColumnName = "ValidTo";
/// });
/// </code>
/// </example>
public sealed class TemporalTableOptions
{
    /// <summary>
    /// Gets or sets the default suffix for history tables.
    /// </summary>
    /// <remarks>
    /// When configuring temporal tables, the history table name defaults to
    /// <c>{TableName}{Suffix}</c>. For example, if the main table is "Orders"
    /// and the suffix is "History", the history table will be "OrdersHistory".
    /// </remarks>
    /// <value>Default: <c>"History"</c></value>
    public string DefaultHistoryTableSuffix { get; set; } = "History";

    /// <summary>
    /// Gets or sets the default schema for history tables.
    /// </summary>
    /// <remarks>
    /// When <c>null</c>, the history table uses the same schema as the main table.
    /// Set this to a specific schema (e.g., "history") to store all history tables
    /// in a separate schema.
    /// </remarks>
    /// <value>Default: <c>null</c> (same schema as main table)</value>
    public string? DefaultHistoryTableSchema { get; set; }

    /// <summary>
    /// Gets or sets the default name for the period start column.
    /// </summary>
    /// <remarks>
    /// SQL Server temporal tables use two hidden period columns to track when each
    /// row was valid. This property controls the name of the "from" column.
    /// </remarks>
    /// <value>Default: <c>"PeriodStart"</c></value>
    public string DefaultPeriodStartColumnName { get; set; } = "PeriodStart";

    /// <summary>
    /// Gets or sets the default name for the period end column.
    /// </summary>
    /// <remarks>
    /// SQL Server temporal tables use two hidden period columns to track when each
    /// row was valid. This property controls the name of the "to" column.
    /// </remarks>
    /// <value>Default: <c>"PeriodEnd"</c></value>
    public string DefaultPeriodEndColumnName { get; set; } = "PeriodEnd";

    /// <summary>
    /// Gets or sets whether to validate that DateTime parameters are in UTC.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see langword="true"/>, temporal query methods validate that DateTime
    /// parameters have <see cref="DateTimeKind.Utc"/>. This helps prevent bugs
    /// caused by accidentally passing local times.
    /// </para>
    /// <para>
    /// <b>Note</b>: SQL Server temporal tables store times in UTC internally.
    /// </para>
    /// </remarks>
    /// <value>Default: <see langword="true"/></value>
    public bool ValidateUtcDateTime { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to log temporal query operations for debugging.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see langword="true"/>, temporal query operations are logged at
    /// <see cref="Microsoft.Extensions.Logging.LogLevel.Debug"/> level.
    /// This is useful for troubleshooting temporal query behavior.
    /// </para>
    /// <para>
    /// <b>Performance Note</b>: Enabling this in production may impact performance
    /// due to additional logging overhead.
    /// </para>
    /// </remarks>
    /// <value>Default: <see langword="false"/></value>
    public bool LogTemporalQueries { get; set; }
}
