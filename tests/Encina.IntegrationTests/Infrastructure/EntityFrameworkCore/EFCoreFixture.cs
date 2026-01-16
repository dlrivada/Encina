using Encina.TestInfrastructure.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore;

/// <summary>
/// EF Core fixture that wraps SqlServerFixture and provides a DbContext.
/// </summary>
public sealed class EFCoreFixture : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlServerFixture = new();

    public TestEFDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestEFDbContext>()
            .UseSqlServer(_sqlServerFixture.ConnectionString)
            .Options;

        return new TestEFDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _sqlServerFixture.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _sqlServerFixture.DisposeAsync();
    }

    /// <summary>
    /// Clears all data from all tables (but preserves schema).
    /// Use this between tests to ensure clean state.
    /// </summary>
    public async Task ClearAllDataAsync()
    {
        await _sqlServerFixture.ClearAllDataAsync();
    }
}
