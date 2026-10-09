using Encina.ADO.SqlServer;
using Encina.IntegrationTests.Messaging.DeadLetter;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.IntegrationTests.ADO.SqlServer.DeadLetter;

/// <summary>
/// Capture, replay and expiry through the ADO.NET registration (<c>UseDeadLetterQueue = true</c>) on a real
/// SQL Server database.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "ADO.SqlServer")]
[Collection("ADO-SqlServer")]
public sealed class DeadLetterEndToEndADOTests
{
    private readonly SqlServerFixture _fixture;

    public DeadLetterEndToEndADOTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DeadLetterQueue_CapturesReplaysAndExpires_ThroughTheRegisteredServices()
    {
        await _fixture.ClearAllDataAsync();
        var (services, clock, encina) = DeadLetterEndToEndScenario.NewServices();
        services.AddSingleton<System.Data.IDbConnection>(_fixture.CreateConnection());
        services.AddEncinaADO(config => config.UseDeadLetterQueue = true);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        await DeadLetterEndToEndScenario.RunAsync(provider, clock, encina);
    }
}
