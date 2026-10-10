using Encina.ADO.MySQL;
using Encina.IntegrationTests.Messaging.DeadLetter;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.IntegrationTests.ADO.MySQL.DeadLetter;

/// <summary>
/// The terminal failure of each source is dead-lettered once through the ADO.NET registration on a real
/// MySQL database (#1991).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("ADO-MySQL")]
public sealed class DeadLetterSourcesEndToEndADOTests : IAsyncLifetime
{
    private readonly MySqlFixture _fixture;

    public DeadLetterSourcesEndToEndADOTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ClearAllDataAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Theory]
    [MemberData(nameof(DeadLetterSourcesEndToEndScenario.Sources), MemberType = typeof(DeadLetterSourcesEndToEndScenario))]
    public async Task Source_TerminalFailure_PersistsOneDeadLetter(string source)
    {
        var (services, clock) = DeadLetterSourcesEndToEndScenario.NewServices();
        using var connection = _fixture.CreateConnection();
        services.AddSingleton<System.Data.IDbConnection>(connection);
        services.AddEncinaADO(DeadLetterSourcesEndToEndScenario.Configure);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        await DeadLetterSourcesEndToEndScenario.RunAsync(source, provider, clock);
    }
}
