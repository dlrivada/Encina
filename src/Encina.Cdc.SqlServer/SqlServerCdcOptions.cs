namespace Encina.Cdc.SqlServer;

/// <summary>
/// Configuration options for the SQL Server Change Tracking CDC connector.
/// </summary>
/// <remarks>
/// <para>
/// SQL Server Change Tracking must be enabled on both the database and the specific tables
/// to be tracked. The connector uses <c>CHANGETABLE(CHANGES ...)</c> to poll for changes.
/// </para>
/// <para>
/// Note: Change Tracking does NOT store old column values. For Update operations,
/// only the <c>After</c> value is available. For Delete operations, only primary key
/// columns are available in the <c>Before</c> value.
/// </para>
/// </remarks>
public sealed class SqlServerCdcOptions
{
    /// <summary>
    /// Gets or sets the SQL Server connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of table names to track for changes.
    /// Table names should include the schema prefix (e.g., "dbo.Orders").
    /// </summary>
    public string[] TrackedTables { get; set; } = [];

    /// <summary>
    /// Gets or sets the default schema name for tables.
    /// Used when table names are specified without a schema prefix.
    /// </summary>
    public string SchemaName { get; set; } = "dbo";

    /// <summary>
    /// Gets or sets the starting Change Tracking version.
    /// When <c>null</c>, starts from the current version (no historical changes).
    /// Set to <c>0</c> to read all available change history.
    /// </summary>
    public long? StartFromVersion { get; set; }
}
