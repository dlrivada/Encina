using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;

/// <summary>
/// Common interface for all EF Core provider fixtures.
/// Provides a consistent API for creating DbContext instances across all database providers.
/// </summary>
public interface IEFCoreFixture : IAsyncLifetime
{
    /// <summary>
    /// Gets the connection string for the underlying database.
    /// </summary>
    string ConnectionString { get; }

    /// <summary>
    /// Gets the provider name (e.g., "SqlServer", "PostgreSQL", "MySQL", "Oracle", "Sqlite").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Creates a new DbContext instance configured for the underlying database provider.
    /// </summary>
    /// <typeparam name="TContext">The type of DbContext to create.</typeparam>
    /// <returns>A new DbContext instance.</returns>
    TContext CreateDbContext<TContext>() where TContext : DbContext;

    /// <summary>
    /// Clears all data from all tables (but preserves schema).
    /// Use this between tests to ensure clean state.
    /// </summary>
    Task ClearAllDataAsync();

    /// <summary>
    /// Ensures the database schema is created using EF Core migrations.
    /// </summary>
    /// <typeparam name="TContext">The type of DbContext to use for migration.</typeparam>
    Task EnsureSchemaCreatedAsync<TContext>() where TContext : DbContext;
}
