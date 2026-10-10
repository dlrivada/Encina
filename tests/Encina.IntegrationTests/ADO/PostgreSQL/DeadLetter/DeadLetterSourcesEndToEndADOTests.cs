using Encina.ADO.PostgreSQL;
using Encina.IntegrationTests.Messaging.DeadLetter;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.IntegrationTests.ADO.PostgreSQL.DeadLetter;

/// <summary>
/// The terminal failure of each source is dead-lettered once through the ADO.NET registration on a real
/// PostgreSQL database (#1991).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("ADO-PostgreSQL")]
public sealed class DeadLetterSourcesEndToEndADOTests
{
    private readonly PostgreSqlFixture _fixture;

    public DeadLetterSourcesEndToEndADOTests(PostgreSqlFixture fixture)
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
