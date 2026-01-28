namespace Encina.ADO.Benchmarks.Infrastructure;

/// <summary>
/// Supported database providers for ADO.NET benchmarks.
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
