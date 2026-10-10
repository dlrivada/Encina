using Encina.EntityFrameworkCore;
using Encina.IntegrationTests.Messaging.DeadLetter;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.SqlServer.DeadLetter;

/// <summary>
/// The terminal failure of each source is dead-lettered once through the EF Core registration on a real
/// SQL Server database (#1991). Every capture runs in a scope of its own, so it never saves the source's changes.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("EFCore-SqlServer")]
public sealed class DeadLetterSourcesEndToEndEFTests : IAsyncLifetime
{
    private readonly EFCoreSqlServerFixture _fixture;

    public DeadLetterSourcesEndToEndEFTests(EFCoreSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.EnsureSchemaCreatedAsync<TestEFDbContext>();
        await _fixture.ClearAllDataAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Theory]
    [MemberData(nameof(DeadLetterSourcesEndToEndScenario.Sources), MemberType = typeof(DeadLetterSourcesEndToEndScenario))]
    public async Task Source_TerminalFailure_PersistsOneDeadLetter(string source)
    {
        var (services, clock) = DeadLetterSourcesEndToEndScenario.NewServices();
        services.AddDbContext<TestEFDbContext>(options => options.UseSqlServer(_fixture.ConnectionString));
        services.AddEncinaEntityFrameworkCore<TestEFDbContext>(DeadLetterSourcesEndToEndScenario.Configure);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        await DeadLetterSourcesEndToEndScenario.RunAsync(source, provider, clock);
    }
}
