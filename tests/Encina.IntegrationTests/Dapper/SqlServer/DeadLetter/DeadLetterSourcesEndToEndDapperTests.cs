using Encina.Dapper.SqlServer;
using Encina.IntegrationTests.Messaging.DeadLetter;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.IntegrationTests.Dapper.SqlServer.DeadLetter;

/// <summary>
/// The terminal failure of each source is dead-lettered once through the Dapper registration on a real
/// SQL Server database (#1991).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("Dapper-SqlServer")]
public sealed class DeadLetterSourcesEndToEndDapperTests
{
    private readonly SqlServerFixture _fixture;

    public DeadLetterSourcesEndToEndDapperTests(SqlServerFixture fixture)
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
        services.AddEncinaDapper(DeadLetterSourcesEndToEndScenario.Configure);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        await DeadLetterSourcesEndToEndScenario.RunAsync(source, provider, clock);
    }
}
