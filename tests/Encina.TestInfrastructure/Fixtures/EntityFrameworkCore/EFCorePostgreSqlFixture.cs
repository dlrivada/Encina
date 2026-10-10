using Microsoft.EntityFrameworkCore;

namespace Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;

/// <summary>
/// EF Core fixture for PostgreSQL that wraps <see cref="PostgreSqlFixture"/>.
/// Provides DbContext creation with Npgsql provider configuration.
/// </summary>
public sealed class EFCorePostgreSqlFixture : IEFCoreFixture
{
    // The EF model creates the DeadLetterMessages table itself, with the unique source key and indexes of
    // DeadLetterMessageConfiguration; the base schema must not create it first.
    private readonly PostgreSqlFixture _postgreSqlFixture = new(includeDeadLetterSchema: false);

    /// <inheritdoc />
    public bool IsAvailable => _postgreSqlFixture.IsAvailable;

    /// <inheritdoc />
    public string ConnectionString => _postgreSqlFixture.ConnectionString;

    /// <inheritdoc />
    public string ProviderName => "PostgreSQL";

    /// <inheritdoc />
    public TContext CreateDbContext<TContext>() where TContext : DbContext
    {
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        optionsBuilder.UseNpgsql(ConnectionString);

        return (TContext)Activator.CreateInstance(
            typeof(TContext),
            optionsBuilder.Options)!;
    }

    /// <inheritdoc />
    public async Task EnsureSchemaCreatedAsync<TContext>() where TContext : DbContext
    {
        await using var context = CreateDbContext<TContext>();
        await EFCoreSchema.CreateTablesAsync(context);
    }

    /// <inheritdoc />
    public async Task ClearAllDataAsync()
    {
        await _postgreSqlFixture.ClearAllDataAsync();
    }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await _postgreSqlFixture.InitializeAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _postgreSqlFixture.DisposeAsync();
    }
}
