using Encina.EntityFrameworkCore;
using Encina.IntegrationTests.Messaging.DeadLetter;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.MySQL.DeadLetter;

/// <summary>
/// The terminal failure of each source is dead-lettered once through the EF Core registration on a real
/// MySQL database (#1991). <see cref="EFCoreMySqlFixture"/> reports the whole MySQL EF Core suite as skipped until
/// Pomelo.EntityFrameworkCore.MySql ships EF Core 10 support (#2086); this class needs no change when it does.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("EFCore-MySQL")]
public sealed class DeadLetterSourcesEndToEndEFTests
{
    private readonly EFCoreMySqlFixture _fixture;

    public DeadLetterSourcesEndToEndEFTests(EFCoreMySqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [MemberData(nameof(DeadLetterSourcesEndToEndScenario.Sources), MemberType = typeof(DeadLetterSourcesEndToEndScenario))]
    public async Task Source_TerminalFailure_PersistsOneDeadLetter(string source)
    {
        await _fixture.EnsureSchemaCreatedAsync<TestEFDbContext>();
        await _fixture.ClearAllDataAsync();
        var (services, clock) = DeadLetterSourcesEndToEndScenario.NewServices();
        services.AddScoped(_ => _fixture.CreateDbContext<TestEFDbContext>());
        services.AddEncinaEntityFrameworkCore<TestEFDbContext>(DeadLetterSourcesEndToEndScenario.Configure);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        await DeadLetterSourcesEndToEndScenario.RunAsync(source, provider, clock);
    }
}
