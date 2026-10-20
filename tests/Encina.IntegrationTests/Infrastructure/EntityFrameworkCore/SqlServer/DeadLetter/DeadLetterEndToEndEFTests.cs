using Encina.EntityFrameworkCore;
using Encina.IntegrationTests.Messaging.DeadLetter;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.SqlServer.DeadLetter;

/// <summary>
/// Capture, replay and expiry through the EF Core registration (<c>UseDeadLetterQueue = true</c>) on a real
/// SQL Server database.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("EFCore-SqlServer")]
public sealed class DeadLetterEndToEndEFTests
{
    private readonly EFCoreSqlServerFixture _fixture;

    public DeadLetterEndToEndEFTests(EFCoreSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DeadLetterQueue_CapturesReplaysAndExpires_ThroughTheRegisteredServices()
    {
        await _fixture.EnsureSchemaCreatedAsync<TestEFDbContext>();
        await _fixture.ClearAllDataAsync();
        var (services, clock, encina) = DeadLetterEndToEndScenario.NewServices();
        services.AddDbContext<TestEFDbContext>(options => options.UseSqlServer(_fixture.ConnectionString));
        services.AddEncinaEntityFrameworkCore<TestEFDbContext>(config => config.UseDeadLetterQueue = true);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        await DeadLetterEndToEndScenario.RunAsync(provider, clock, encina);
    }
}
