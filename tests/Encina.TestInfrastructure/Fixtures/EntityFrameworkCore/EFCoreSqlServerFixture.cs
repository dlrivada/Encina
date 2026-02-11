using Microsoft.EntityFrameworkCore;

namespace Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;

/// <summary>
/// EF Core fixture for SQL Server that wraps <see cref="SqlServerFixture"/>.
/// Provides DbContext creation with SQL Server provider configuration.
/// </summary>
public sealed class EFCoreSqlServerFixture : IEFCoreFixture
{
    private readonly SqlServerFixture _sqlServerFixture = new();

    /// <inheritdoc />
    public bool IsAvailable => _sqlServerFixture.IsAvailable;

    /// <inheritdoc />
    public string ConnectionString => _sqlServerFixture.ConnectionString;

    /// <inheritdoc />
    public string ProviderName => "SqlServer";

    /// <inheritdoc />
    public TContext CreateDbContext<TContext>() where TContext : DbContext
    {
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        optionsBuilder.UseSqlServer(ConnectionString);

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
        await _sqlServerFixture.ClearAllDataAsync();
    }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await _sqlServerFixture.InitializeAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _sqlServerFixture.DisposeAsync();
    }
}
