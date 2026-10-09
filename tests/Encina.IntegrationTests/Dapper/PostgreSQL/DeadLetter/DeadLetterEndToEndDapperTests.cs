using Encina.Dapper.PostgreSQL;
using Encina.IntegrationTests.Messaging.DeadLetter;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.IntegrationTests.Dapper.PostgreSQL.DeadLetter;

/// <summary>
/// Capture, replay and expiry through the Dapper registration (<c>UseDeadLetterQueue = true</c>) on a real
/// PostgreSQL database.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "Dapper.PostgreSQL")]
[Collection("Dapper-PostgreSQL")]
public sealed class DeadLetterEndToEndDapperTests
{
    private readonly PostgreSqlFixture _fixture;

    public DeadLetterEndToEndDapperTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DeadLetterQueue_CapturesReplaysAndExpires_ThroughTheRegisteredServices()
    {
        await _fixture.ClearAllDataAsync();
        var (services, clock, encina) = DeadLetterEndToEndScenario.NewServices();
        services.AddSingleton<System.Data.IDbConnection>(_fixture.CreateConnection());
        services.AddEncinaDapper(config => config.UseDeadLetterQueue = true);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        await DeadLetterEndToEndScenario.RunAsync(provider, clock, encina);
    }
}
