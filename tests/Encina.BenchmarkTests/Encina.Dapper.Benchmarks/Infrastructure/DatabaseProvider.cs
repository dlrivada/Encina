namespace Encina.Dapper.Benchmarks.Infrastructure;

/// <summary>
/// Supported database providers for Dapper benchmarks.
/// </summary>
public enum DatabaseProvider
{
    /// <summary>
    /// SQLite database (in-memory or file-based).
    /// </summary>
    Sqlite,

    /// <summary>
    /// Microsoft SQL Server.
    /// </summary>
    SqlServer,

    /// <summary>
    /// PostgreSQL database.
    /// </summary>
    PostgreSql,

    /// <summary>
    /// MySQL database.
    /// </summary>
    MySql
}
