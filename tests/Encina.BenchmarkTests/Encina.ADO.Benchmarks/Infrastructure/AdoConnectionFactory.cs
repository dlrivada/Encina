using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Npgsql;
using MySqlConnector;

namespace Encina.ADO.Benchmarks.Infrastructure;

/// <summary>
/// Factory for creating database connections for ADO.NET benchmarks.
/// </summary>
public static class AdoConnectionFactory
{
    /// <summary>
    /// Creates an in-memory SQLite connection for benchmarks.
    /// This is the fastest option for benchmarking as it avoids disk I/O.
    /// </summary>
    /// <returns>An open SQLite connection.</returns>
    public static SqliteConnection CreateInMemorySqliteConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Creates a shared in-memory SQLite connection that persists across queries.
    /// Uses cache=shared mode so the database survives connection close/reopen.
    /// </summary>
    /// <param name="databaseName">The name for the shared cache database.</param>
    /// <returns>An open SQLite connection.</returns>
    public static SqliteConnection CreateSharedMemorySqliteConnection(string databaseName = "benchmark")
    {
        var connection = new SqliteConnection($"Data Source={databaseName};Mode=Memory;Cache=Shared");
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Creates a SQL Server connection.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>A SQL Server connection (not opened).</returns>
    public static SqlConnection CreateSqlServerConnection(string connectionString)
    {
        return new SqlConnection(connectionString);
    }

    /// <summary>
    /// Creates a PostgreSQL connection.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>A PostgreSQL connection (not opened).</returns>
    public static NpgsqlConnection CreatePostgreSqlConnection(string connectionString)
    {
        return new NpgsqlConnection(connectionString);
    }

    /// <summary>
    /// Creates a MySQL connection.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>A MySQL connection (not opened).</returns>
    public static MySqlConnection CreateMySqlConnection(string connectionString)
    {
        return new MySqlConnection(connectionString);
    }

    /// <summary>
    /// Creates a connection for the specified provider.
    /// </summary>
    /// <param name="provider">The database provider.</param>
    /// <param name="connectionString">The connection string (optional for SQLite in-memory).</param>
    /// <returns>A database connection.</returns>
    public static IDbConnection CreateConnection(DatabaseProvider provider, string? connectionString = null)
    {
        return provider switch
        {
            DatabaseProvider.Sqlite => string.IsNullOrEmpty(connectionString)
                ? CreateInMemorySqliteConnection()
                : new SqliteConnection(connectionString),
            DatabaseProvider.SqlServer => CreateSqlServerConnection(
                connectionString ?? throw new ArgumentNullException(nameof(connectionString), "Connection string required for SQL Server")),
            DatabaseProvider.PostgreSql => CreatePostgreSqlConnection(
                connectionString ?? throw new ArgumentNullException(nameof(connectionString), "Connection string required for PostgreSQL")),
            DatabaseProvider.MySql => CreateMySqlConnection(
                connectionString ?? throw new ArgumentNullException(nameof(connectionString), "Connection string required for MySQL")),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported database provider")
        };
    }

    /// <summary>
    /// Gets the default connection string for Testcontainers integration.
    /// </summary>
    /// <param name="provider">The database provider.</param>
    /// <param name="host">The host name.</param>
    /// <param name="port">The port number.</param>
    /// <param name="database">The database name.</param>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <returns>A connection string for the specified provider.</returns>
    public static string GetConnectionString(
        DatabaseProvider provider,
        string host,
        int port,
        string database,
        string username,
        string password)
    {
        return provider switch
        {
            DatabaseProvider.SqlServer => $"Server={host},{port};Database={database};User Id={username};Password={password};TrustServerCertificate=True",
            DatabaseProvider.PostgreSql => $"Host={host};Port={port};Database={database};Username={username};Password={password}",
            DatabaseProvider.MySql => $"Server={host};Port={port};Database={database};User={username};Password={password}",
            DatabaseProvider.Sqlite => $"Data Source={database}",
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported database provider")
        };
    }
}
