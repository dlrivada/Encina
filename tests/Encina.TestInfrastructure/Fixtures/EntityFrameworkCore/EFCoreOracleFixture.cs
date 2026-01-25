using Microsoft.EntityFrameworkCore;

namespace Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;

/// <summary>
/// EF Core fixture for Oracle that wraps <see cref="OracleFixture"/>.
/// Provides DbContext creation with Oracle provider configuration.
/// </summary>
public sealed class EFCoreOracleFixture : IEFCoreFixture
{
    private readonly OracleFixture _oracleFixture = new();

    /// <inheritdoc />
    public string ConnectionString => _oracleFixture.ConnectionString;

    /// <inheritdoc />
    public string ProviderName => "Oracle";

    /// <inheritdoc />
    public TContext CreateDbContext<TContext>() where TContext : DbContext
    {
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        optionsBuilder.UseOracle(ConnectionString);

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
        await _oracleFixture.ClearAllDataAsync();
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _oracleFixture.InitializeAsync();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await _oracleFixture.DisposeAsync();
    }
}
