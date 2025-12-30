using System.Data.Common;
using Npgsql;

namespace Encina.Testing.Respawn;

/// <summary>
/// Respawn implementation for PostgreSQL databases.
/// </summary>
/// <remarks>
/// <para>
/// Provides intelligent database cleanup for PostgreSQL integration tests.
/// Supports PostgreSQL-specific features and schemas.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class PostgreSqlTests : IClassFixture&lt;PostgreSqlRespawner&gt;, IAsyncLifetime
/// {
///     private readonly PostgreSqlRespawner _respawner;
///
///     public PostgreSqlTests(PostgreSqlRespawner respawner)
///     {
///         _respawner = respawner;
///         _respawner.Options.SchemasToInclude = ["public"];
///     }
///
///     public Task InitializeAsync() => _respawner.InitializeAsync();
///     public Task DisposeAsync() => _respawner.ResetAsync();
/// }
/// </code>
/// </example>
public sealed class PostgreSqlRespawner : DatabaseRespawner
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlRespawner"/> class.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    public PostgreSqlRespawner(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    protected override string ConnectionString => _connectionString;

    /// <inheritdoc />
    protected override RespawnAdapter Adapter => RespawnAdapter.PostgreSql;

    /// <inheritdoc />
    protected override DbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    /// <summary>
    /// Creates a new PostgreSqlRespawner from a connection string builder.
    /// </summary>
    /// <param name="builder">The connection string builder.</param>
    /// <returns>A new PostgreSqlRespawner instance.</returns>
    public static PostgreSqlRespawner FromBuilder(NpgsqlConnectionStringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return new PostgreSqlRespawner(builder.ConnectionString);
    }
}
