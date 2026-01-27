using System.Data;
using Encina.Tenancy;

namespace Encina.NBomber.Scenarios.Database;

/// <summary>
/// Factory for creating database-specific services for load testing scenarios.
/// Each provider implementation wraps a TestInfrastructure fixture and provides
/// access to Unit of Work, Tenancy, and Read/Write Separation capabilities.
/// </summary>
public interface IDatabaseProviderFactory : IAsyncDisposable
{
    /// <summary>
    /// Gets the unique provider name (e.g., "ado-postgresql", "dapper-sqlite", "efcore-sqlserver", "mongodb").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets the provider category (ADO, Dapper, EFCore, MongoDB).
    /// </summary>
    ProviderCategory Category { get; }

    /// <summary>
    /// Gets the database type (Sqlite, SqlServer, PostgreSQL, MySQL, MongoDB).
    /// </summary>
    DatabaseType DatabaseType { get; }

    /// <summary>
    /// Gets a value indicating whether this provider supports Read/Write separation.
    /// MongoDB does not support read/write separation in the same way as relational databases.
    /// </summary>
    bool SupportsReadWriteSeparation { get; }

    /// <summary>
    /// Gets the connection string for the underlying database.
    /// </summary>
    string ConnectionString { get; }

    /// <summary>
    /// Initializes the database provider, starting any required containers
    /// and creating the database schema.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new database connection.
    /// </summary>
    /// <returns>An open database connection.</returns>
    IDbConnection CreateConnection();

    /// <summary>
    /// Creates a Unit of Work instance for transaction management.
    /// </summary>
    /// <returns>A Unit of Work instance, or null if not supported.</returns>
    object? CreateUnitOfWork();

    /// <summary>
    /// Creates a tenant provider for multi-tenancy scenarios.
    /// </summary>
    /// <returns>A tenant provider instance.</returns>
    ITenantProvider CreateTenantProvider();

    /// <summary>
    /// Creates a read/write connection selector for read/write separation scenarios.
    /// </summary>
    /// <returns>A read/write selector, or null if not supported.</returns>
    object? CreateReadWriteSelector();

    /// <summary>
    /// Clears all data from the database while preserving the schema.
    /// Used between test runs for clean state.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearDataAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Provider category enumeration.
/// </summary>
public enum ProviderCategory
{
    /// <summary>ADO.NET raw implementation.</summary>
    ADO,

    /// <summary>Dapper micro-ORM implementation.</summary>
    Dapper,

    /// <summary>Entity Framework Core implementation.</summary>
    EFCore,

    /// <summary>MongoDB document database implementation.</summary>
    MongoDB
}

/// <summary>
/// Database type enumeration.
/// </summary>
public enum DatabaseType
{
    /// <summary>SQLite (file-based, no container).</summary>
    Sqlite,

    /// <summary>Microsoft SQL Server.</summary>
    SqlServer,

    /// <summary>PostgreSQL.</summary>
    PostgreSQL,

    /// <summary>MySQL/MariaDB.</summary>
    MySQL,

    /// <summary>MongoDB document database.</summary>
    MongoDB
}
