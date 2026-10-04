using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.ABAC;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Encina.IntegrationTests.Security.ABAC.EFCore;

/// <summary>
/// Registration test (#1707): the persistent PAP works with the real EF Core (SQL Server)
/// registration, whose <c>IPolicyStore</c> is scoped over a scoped <see cref="DbContext"/>.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("EFCore-SqlServer")]
public sealed class PersistentPapEFCoreSqlServerRegistrationTests(EFCoreSqlServerFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        await fixture.ClearAllDataAsync();

        await using var dbContext = fixture.CreateDbContext<RegistrationDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task PersistentPap_WithScopedEFCorePolicyStore_BuildsSeedsAndChangesPolicies()
    {
        var services = new ServiceCollection();
        services.AddDbContext<RegistrationDbContext>(options => options.UseSqlServer(fixture.ConnectionString));
        services.AddEncinaEntityFrameworkCore<RegistrationDbContext>(config => config.UseABACPolicyStore = true);

        await PersistentPapScopeScenario.RunAsync(services);
    }

    private sealed class RegistrationDbContext(DbContextOptions<RegistrationDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyABACConfiguration();
    }
}
