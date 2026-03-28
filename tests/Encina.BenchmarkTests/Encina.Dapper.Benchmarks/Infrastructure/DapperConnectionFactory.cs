using System.Data;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;

namespace Encina.Dapper.Benchmarks.Infrastructure;

/// <summary>
/// Factory for creating database connections for Dapper benchmarks.
/// </summary>
public static class DapperConnectionFactory
{
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
    /// <param name="connectionString">The connection string.</param>
    /// <returns>A database connection.</returns>
    public static IDbConnection CreateConnection(DatabaseProvider provider, string? connectionString = null)
    {
        return provider switch
        {
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
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported database provider")
        };
    }
}
