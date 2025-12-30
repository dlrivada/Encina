using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Encina.Testing.Respawn;

/// <summary>
/// Respawn implementation for SQL Server databases.
/// </summary>
/// <remarks>
/// <para>
/// Provides intelligent database cleanup for SQL Server integration tests.
/// Supports all SQL Server features including temporal tables and identity reseeding.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class SqlServerTests : IClassFixture&lt;SqlServerRespawner&gt;, IAsyncLifetime
/// {
///     private readonly SqlServerRespawner _respawner;
///
///     public SqlServerTests(SqlServerRespawner respawner)
///     {
///         _respawner = respawner;
///         _respawner.Options.TablesToIgnore = ["__EFMigrationsHistory"];
///     }
///
///     public Task InitializeAsync() => _respawner.InitializeAsync();
///     public Task DisposeAsync() => _respawner.ResetAsync();
/// }
/// </code>
/// </example>
public sealed class SqlServerRespawner : DatabaseRespawner
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerRespawner"/> class.
    /// </summary>
    /// <param name="connectionString">The SQL Server connection string.</param>
    public SqlServerRespawner(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    protected override string ConnectionString => _connectionString;

    /// <inheritdoc />
    protected override RespawnAdapter Adapter => RespawnAdapter.SqlServer;

    /// <inheritdoc />
    protected override DbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    /// <summary>
    /// Creates a new SqlServerRespawner from a connection string builder.
    /// </summary>
    /// <param name="builder">The connection string builder.</param>
    /// <returns>A new SqlServerRespawner instance.</returns>
    public static SqlServerRespawner FromBuilder(SqlConnectionStringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return new SqlServerRespawner(builder.ConnectionString);
    }
}
