using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;

/// <summary>
/// EF Core fixture for SQLite that wraps <see cref="SqliteFixture"/>.
/// Provides DbContext creation with SQLite provider configuration.
/// </summary>
/// <remarks>
/// SQLite uses an in-memory database, so all operations occur on the same connection.
/// The connection is kept open for the lifetime of the fixture.
/// </remarks>
public sealed class EFCoreSqliteFixture : IEFCoreFixture
{
    private readonly SqliteFixture _sqliteFixture = new();

    /// <inheritdoc />
    public string ConnectionString => _sqliteFixture.ConnectionString;

    /// <inheritdoc />
    public string ProviderName => "Sqlite";

    /// <inheritdoc />
    public TContext CreateDbContext<TContext>() where TContext : DbContext
    {
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();

        // For SQLite in-memory, we need to use the same connection
        // to keep the database alive. Cast to SqliteConnection as UseSqlite
        // requires DbConnection, not IDbConnection.
        var connection = (SqliteConnection)_sqliteFixture.CreateConnection();
        optionsBuilder.UseSqlite(connection);

        return (TContext)Activator.CreateInstance(
            typeof(TContext),
            optionsBuilder.Options)!;
    }

    /// <inheritdoc />
    public async Task EnsureSchemaCreatedAsync<TContext>() where TContext : DbContext
    {
        await using var context = CreateDbContext<TContext>();
        await context.Database.EnsureCreatedAsync();
    }

    /// <inheritdoc />
    public async Task ClearAllDataAsync()
    {
        await _sqliteFixture.ClearAllDataAsync();
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _sqliteFixture.InitializeAsync();
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        return _sqliteFixture.DisposeAsync();
    }
}
