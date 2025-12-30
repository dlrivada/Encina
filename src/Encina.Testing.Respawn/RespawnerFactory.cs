namespace Encina.Testing.Respawn;

using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;

/// <summary>
/// Factory for creating database respawners based on provider type.
/// </summary>
/// <remarks>
/// Provides a convenient way to create respawners without knowing the concrete type.
/// </remarks>
/// <example>
/// <code>
/// var respawner = RespawnerFactory.Create(
///     RespawnAdapter.SqlServer,
///     connectionString);
///
/// await respawner.ResetAsync();
/// </code>
/// </example>
public static class RespawnerFactory
{
    private static int _verboseDiagnostics;
    private static Action<string, string, string?>? _diagnosticsCallback;

    private static readonly bool EnvVerboseFlag =
        Environment.GetEnvironmentVariable("ENCINA_RESPAWN_VERBOSE") is "1" or "true";

    /// <summary>
    /// Gets or sets whether verbose diagnostics are enabled for connection string parsing.
    /// When enabled, parsing exceptions are logged via <see cref="DiagnosticsCallback"/>.
    /// Can also be enabled via the ENCINA_RESPAWN_VERBOSE environment variable.
    /// </summary>
    public static bool VerboseDiagnostics
    {
        get => Interlocked.CompareExchange(ref _verboseDiagnostics, 0, 0) == 1;
        set => Interlocked.Exchange(ref _verboseDiagnostics, value ? 1 : 0);
    }

    /// <summary>
    /// Gets or sets an optional callback for diagnostic messages during connection string parsing.
    /// Called with provider name, exception message, and stack trace when parsing fails.
    /// </summary>
    public static Action<string, string, string?>? DiagnosticsCallback
    {
        get => Interlocked.CompareExchange(ref _diagnosticsCallback, null, null);
        set => Interlocked.Exchange(ref _diagnosticsCallback, value);
    }

    private static bool IsVerbose => VerboseDiagnostics || EnvVerboseFlag;

    /// <summary>
    /// Creates a respawner for the specified database adapter.
    /// </summary>
    /// <param name="adapter">The database adapter type.</param>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="options">Optional configuration options.</param>
    /// <returns>A configured database respawner.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the specified adapter is not supported.
    /// </exception>
    public static DatabaseRespawner Create(
        RespawnAdapter adapter,
        string connectionString,
        RespawnOptions? options = null)
    {
        DatabaseRespawner respawner = adapter switch
        {
            RespawnAdapter.SqlServer => new SqlServerRespawner(connectionString),
            RespawnAdapter.PostgreSql => new PostgreSqlRespawner(connectionString),
            RespawnAdapter.MySql => new MySqlRespawner(connectionString),
            RespawnAdapter.Oracle => throw new NotSupportedException(
                "Oracle respawner requires the Oracle.ManagedDataAccess.Core package. " +
                "Use OracleRespawner directly if available."),
            _ => throw new NotSupportedException($"Database adapter {adapter} is not supported")
        };

        if (options is not null)
        {
            respawner.Options = options;
        }

        return respawner;
    }

    /// <summary>
    /// Creates a SQLite respawner.
    /// </summary>
    /// <param name="connectionString">The SQLite connection string.</param>
    /// <param name="options">Optional configuration options.</param>
    /// <returns>A configured SQLite respawner.</returns>
    /// <remarks>
    /// SQLite uses a separate respawner implementation since Respawn
    /// does not officially support SQLite.
    /// </remarks>
    public static SqliteRespawner CreateSqlite(
        string connectionString,
        RespawnOptions? options = null)
    {
        var respawner = new SqliteRespawner(connectionString);

        if (options is not null)
        {
            respawner.Options = options;
        }

        return respawner;
    }

    /// <summary>
    /// Infers the database adapter from the connection string using provider-specific parsers.
    /// </summary>
    /// <param name="connectionString">The database connection string.</param>
    /// <returns>The inferred database adapter, or null if not recognized.</returns>
    /// <remarks>
    /// <para>
    /// This method attempts to parse the connection string using each provider's
    /// <see cref="System.Data.Common.DbConnectionStringBuilder"/> to determine the adapter type.
    /// It checks providers in order of specificity: PostgreSQL, MySQL, SQL Server, SQLite.
    /// </para>
    /// <para>
    /// <b>Note:</b> This is a best-effort detection that may produce incorrect results with
    /// non-standard or ambiguous connection strings. For reliable operation, use
    /// <see cref="Create(RespawnAdapter, string, RespawnOptions?)"/> with an explicit adapter.
    /// </para>
    /// <para>
    /// Oracle is not inferred because its connection string builder requires a separate package
    /// (Oracle.ManagedDataAccess.Core).
    /// </para>
    /// </remarks>
    public static RespawnAdapter? InferAdapter(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        // Try PostgreSQL first - NpgsqlConnectionStringBuilder is strict about valid keywords
        if (TryParsePostgreSql(connectionString))
        {
            return RespawnAdapter.PostgreSql;
        }

        // Try MySQL - MySqlConnectionStringBuilder validates MySQL-specific keywords
        if (TryParseMySql(connectionString))
        {
            return RespawnAdapter.MySql;
        }

        // Try SQL Server - SqlConnectionStringBuilder is more permissive
        if (TryParseSqlServer(connectionString))
        {
            return RespawnAdapter.SqlServer;
        }

        // SQLite is not returned here since it uses a separate respawner (SqliteRespawner)
        // and Create() only handles DatabaseRespawner types

        return null;
    }

    private static bool TryParsePostgreSql(string connectionString)
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            // Require Host to be set - this is mandatory for PostgreSQL
            return !string.IsNullOrEmpty(builder.Host);
        }
        catch (Exception ex)
        {
            LogParseFailure("PostgreSQL", ex);
            return false;
        }
    }

    private static bool TryParseMySql(string connectionString)
    {
        try
        {
            var builder = new MySqlConnectionStringBuilder(connectionString);
            // Require Server to be set - this is mandatory for MySQL
            return !string.IsNullOrEmpty(builder.Server);
        }
        catch (Exception ex)
        {
            LogParseFailure("MySQL", ex);
            return false;
        }
    }

    private static bool TryParseSqlServer(string connectionString)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            // Require DataSource to be set - this is mandatory for SQL Server
            return !string.IsNullOrEmpty(builder.DataSource);
        }
        catch (Exception ex)
        {
            LogParseFailure("SqlServer", ex);
            return false;
        }
    }

    private static void LogParseFailure(string provider, Exception ex)
    {
        if (!IsVerbose)
        {
            return;
        }

        var message = $"[Encina.Testing.Respawn] Failed to parse connection string as {provider}: {ex.Message}";

        // Capture callback to local variable to avoid race conditions
        var callback = DiagnosticsCallback;
        callback?.Invoke(provider, ex.Message, ex.StackTrace);

        // Also write to debug output
        Debug.WriteLine(message);
        Debug.WriteLineIf(ex.StackTrace is not null, ex.StackTrace);
    }
}
