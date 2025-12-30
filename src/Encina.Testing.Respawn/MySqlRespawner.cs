using System.Data.Common;
using MySqlConnector;

namespace Encina.Testing.Respawn;

/// <summary>
/// Respawn implementation for MySQL databases.
/// </summary>
/// <remarks>
/// <para>
/// Provides intelligent database cleanup for MySQL integration tests.
/// Supports MySQL-specific features including InnoDB foreign key handling.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MySqlTests : IClassFixture&lt;MySqlRespawner&gt;, IAsyncLifetime
/// {
///     private readonly MySqlRespawner _respawner;
///
///     public MySqlTests(MySqlRespawner respawner)
///     {
///         _respawner = respawner;
///     }
///
///     public Task InitializeAsync() => _respawner.InitializeAsync();
///     public Task DisposeAsync() => _respawner.ResetAsync();
/// }
/// </code>
/// </example>
public sealed class MySqlRespawner : DatabaseRespawner
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlRespawner"/> class.
    /// </summary>
    /// <param name="connectionString">The MySQL connection string.</param>
    public MySqlRespawner(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    protected override string ConnectionString => _connectionString;

    /// <inheritdoc />
    protected override RespawnAdapter Adapter => RespawnAdapter.MySql;

    /// <inheritdoc />
    protected override DbConnection CreateConnection()
    {
        return new MySqlConnection(_connectionString);
    }

    /// <summary>
    /// Creates a new MySqlRespawner from a connection string builder.
    /// </summary>
    /// <param name="builder">The connection string builder.</param>
    /// <returns>A new MySqlRespawner instance.</returns>
    public static MySqlRespawner FromBuilder(MySqlConnectionStringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return new MySqlRespawner(builder.ConnectionString);
    }
}
