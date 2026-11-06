using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.MySQL.Inbox;

/// <summary>
/// Runs the hosted <c>MessagePurgeService</c> end to end over the EF Core MySQL inbox store (#3046).
/// Skipped, like the other EF Core MySQL tests, until the Pomelo provider supports EF Core 10 (#2086).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("EFCore-MySQL")]
public sealed class MessagePurgeServiceEFMySqlTests : IAsyncLifetime
{
    private readonly EFCoreMySqlFixture _fixture;

    public MessagePurgeServiceEFMySqlTests(EFCoreMySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ClearAllDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task PurgeCycle_ExpiredAndLiveRows_RemovesOnlyTheProcessedExpiredRow()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility (#2086)");

        // Enable together with the other MySQL tests once the Pomelo provider supports EF Core 10 (#2086):
        // pass o => o.UseMySql(...) as the database configuration.
        await global::Encina.IntegrationTests.Messaging.MessagePurgeScenario.AssertCycleRemovesOnlyProcessedExpiredRowsAsync(
            global::Encina.IntegrationTests.Messaging.InboxPipelineHarnesses.Ef<TestEFDbContext>(
                _fixture.ConnectionString,
                (_, _) => { /* o.UseMySql(...) once the Pomelo provider supports EF Core 10 (#2086) */ },
                () => _fixture.CreateDbContext<TestEFDbContext>()));
    }
}
