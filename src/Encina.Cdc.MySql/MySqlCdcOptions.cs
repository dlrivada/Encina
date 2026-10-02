using System.Text.Json.Serialization;

namespace Encina.Cdc.MySql;

/// <summary>
/// Configuration options for the MySQL Binary Log Replication CDC connector.
/// </summary>
/// <remarks>
/// <para>
/// MySQL binary log replication requires:
/// <list type="bullet">
///   <item><description><c>binlog_format=ROW</c> (default in MySQL 8+)</description></item>
///   <item><description><c>REPLICATION SLAVE</c> and <c>REPLICATION CLIENT</c> privileges for the user</description></item>
///   <item><description>Unique <see cref="ServerId"/> per replication client</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class MySqlCdcOptions
{
    /// <summary>
    /// Gets or sets the MySQL connection string (used to read the current binlog position).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Required. Every server in it must use TCP and must not target a loopback host (unless
    /// <see cref="AllowLocalEndpoints"/> is set) or a link-local, cloud metadata or unspecified address.
    /// Unix socket, named pipe and shared memory connections count as local endpoints.
    /// </para>
    /// <para>WARNING: Contains sensitive credential data. Never log or serialize.</para>
    /// </remarks>
    [JsonIgnore]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MySQL server hostname for binlog replication.
    /// </summary>
    /// <remarks>
    /// Must not target a loopback host (unless <see cref="AllowLocalEndpoints"/> is set) or a
    /// link-local, cloud metadata or unspecified address. The default <c>localhost</c> is therefore
    /// rejected at registration unless <see cref="AllowLocalEndpoints"/> is set.
    /// </remarks>
    public string Hostname { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="Hostname"/> and the servers in
    /// <see cref="ConnectionString"/> may target <c>localhost</c>, a loopback address or a local socket.
    /// </summary>
    /// <value>
    /// Defaults to <c>false</c>. Set to <c>true</c> for local development or a database on the same
    /// host; a warning is logged at startup when it is set.
    /// </value>
    public bool AllowLocalEndpoints { get; set; }

    /// <summary>
    /// Gets or sets the MySQL server port for binlog replication.
    /// </summary>
    public int Port { get; set; } = 3306;

    /// <summary>
    /// Gets or sets the MySQL username for binlog replication.
    /// The user must have REPLICATION SLAVE and REPLICATION CLIENT privileges.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MySQL password for binlog replication.
    /// </summary>
    /// <remarks>
    /// WARNING: Contains sensitive credential data. Never log or serialize.
    /// </remarks>
    [JsonIgnore]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique server ID for this replication client.
    /// Must be unique across all replication clients connected to the same server.
    /// </summary>
    public long ServerId { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether to use GTID-based replication.
    /// When <c>true</c>, positions are tracked using GTIDs.
    /// When <c>false</c>, positions use file/position tracking.
    /// Default is <c>true</c>.
    /// </summary>
    public bool UseGtid { get; set; } = true;

    /// <summary>
    /// Gets or sets the database names to include in replication.
    /// When empty, all databases are included.
    /// </summary>
    public string[] IncludeDatabases { get; set; } = [];

    /// <summary>
    /// Gets or sets the table names to include in replication.
    /// Format: <c>"database.table"</c>. When empty, all tables are included.
    /// </summary>
    public string[] IncludeTables { get; set; } = [];

    /// <inheritdoc/>
    public override string ToString() =>
        $"MySqlCdcOptions {{ Host={Hostname}:{Port}, ServerId={ServerId} }}";
}
