using Encina.EntityFrameworkCore;
using Encina.IntegrationTests.Messaging.DeadLetter;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.PostgreSQL.DeadLetter;

/// <summary>
/// The terminal failure of each source is dead-lettered once through the EF Core registration on a real
/// PostgreSQL database (#1991). Every capture runs in a scope of its own, so it never saves the source's changes.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("EFCore-PostgreSQL")]
public sealed class DeadLetterSourcesEndToEndEFTests
{
    private readonly EFCorePostgreSqlFixture _fixture;

    public DeadLetterSourcesEndToEndEFTests(EFCorePostgreSqlFixture fixture)
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
        services.AddDbContext<TestEFDbContext>(options => options.UseNpgsql(_fixture.ConnectionString));
        services.AddEncinaEntityFrameworkCore<TestEFDbContext>(DeadLetterSourcesEndToEndScenario.Configure);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        await DeadLetterSourcesEndToEndScenario.RunAsync(source, provider, clock);
    }
}
