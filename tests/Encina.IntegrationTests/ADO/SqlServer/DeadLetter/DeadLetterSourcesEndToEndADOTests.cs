using Encina.ADO.SqlServer;
using Encina.IntegrationTests.Messaging.DeadLetter;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.IntegrationTests.ADO.SqlServer.DeadLetter;

/// <summary>
/// The terminal failure of each source is dead-lettered once through the ADO.NET registration on a real
/// SQL Server database (#1991).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("ADO-SqlServer")]
public sealed class DeadLetterSourcesEndToEndADOTests
{
    private readonly SqlServerFixture _fixture;

    public DeadLetterSourcesEndToEndADOTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [MemberData(nameof(DeadLetterSourcesEndToEndScenario.Sources), MemberType = typeof(DeadLetterSourcesEndToEndScenario))]
    public async Task Source_TerminalFailure_PersistsOneDeadLetter(string source)
    {
        await _fixture.ClearAllDataAsync();
        var (services, clock) = DeadLetterSourcesEndToEndScenario.NewServices();
        using var connection = _fixture.CreateConnection();
        services.AddSingleton<System.Data.IDbConnection>(connection);
        services.AddEncinaADO(DeadLetterSourcesEndToEndScenario.Configure);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        await DeadLetterSourcesEndToEndScenario.RunAsync(source, provider, clock);
    }
}
