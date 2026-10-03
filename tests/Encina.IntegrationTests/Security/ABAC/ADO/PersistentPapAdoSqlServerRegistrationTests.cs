using Encina.ADO.SqlServer;
using Encina.TestInfrastructure.Fixtures;

namespace Encina.IntegrationTests.Security.ABAC.ADO;

/// <summary>
/// Registration test (#1707): the persistent PAP works with the real ADO.NET (SQL Server)
/// registration, whose <c>IPolicyStore</c> is scoped.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("ADO-SqlServer")]
public sealed class PersistentPapAdoSqlServerRegistrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ClearAllDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task PersistentPap_WithScopedAdoPolicyStore_BuildsSeedsAndChangesPolicies()
    {
        var services = new ServiceCollection();
        services.AddEncinaADO(fixture.ConnectionString, config => config.UseABACPolicyStore = true);

        await PersistentPapScopeScenario.RunAsync(services);
    }
}
