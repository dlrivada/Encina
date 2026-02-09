namespace Encina.Cdc.PostgreSql;

/// <summary>
/// Configuration options for the PostgreSQL Logical Replication CDC connector.
/// </summary>
/// <remarks>
/// <para>
/// PostgreSQL Logical Replication requires:
/// <list type="bullet">
///   <item><description><c>wal_level = logical</c> in postgresql.conf</description></item>
///   <item><description>A publication for the tables to track</description></item>
///   <item><description>A replication slot for the connector</description></item>
///   <item><description><c>REPLICA IDENTITY FULL</c> on tables for before-values on updates/deletes</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class PostgresCdcOptions
{
    /// <summary>
    /// Gets or sets the PostgreSQL connection string.
    /// Must include replication permissions for the user.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the PostgreSQL publication to subscribe to.
    /// </summary>
    public string PublicationName { get; set; } = "encina_cdc_publication";

    /// <summary>
    /// Gets or sets the name of the replication slot.
    /// </summary>
    public string ReplicationSlotName { get; set; } = "encina_cdc_slot";

    /// <summary>
    /// Gets or sets whether to create the replication slot if it does not exist.
    /// Default is <c>true</c>.
    /// </summary>
    public bool CreateSlotIfNotExists { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to create the publication if it does not exist.
    /// Default is <c>true</c>.
    /// </summary>
    public bool CreatePublicationIfNotExists { get; set; } = true;

    /// <summary>
    /// Gets or sets the table names to include in the publication.
    /// Used when <see cref="CreatePublicationIfNotExists"/> is <c>true</c>.
    /// Format: <c>"schema.table"</c> (e.g., <c>"public.orders"</c>).
    /// </summary>
    public string[] PublicationTables { get; set; } = [];
}
