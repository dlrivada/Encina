using Encina.Dapper.SqlServer;
using Encina.TestInfrastructure.Fixtures;

namespace Encina.IntegrationTests.Security.ABAC.Dapper;

/// <summary>
/// Registration test (#1707): the persistent PAP works with the real Dapper (SQL Server)
/// registration, whose <c>IPolicyStore</c> is scoped.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("Dapper-SqlServer")]
public sealed class PersistentPapDapperSqlServerRegistrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ClearAllDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task PersistentPap_WithScopedDapperPolicyStore_BuildsSeedsAndChangesPolicies()
    {
        var services = new ServiceCollection();
        services.AddEncinaDapper(fixture.ConnectionString, config => config.UseABACPolicyStore = true);

        await PersistentPapScopeScenario.RunAsync(services);
    }
}
