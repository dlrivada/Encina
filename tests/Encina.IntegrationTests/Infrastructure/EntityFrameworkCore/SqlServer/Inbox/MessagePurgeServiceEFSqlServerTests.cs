using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.SqlServer.Inbox;

/// <summary>
/// Runs the hosted <c>MessagePurgeService</c> end to end over the EF Core SQL Server inbox store (#3046).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("EFCore-SqlServer")]
public sealed class MessagePurgeServiceEFSqlServerTests : IAsyncLifetime
{
    private readonly EFCoreSqlServerFixture _fixture;

    public MessagePurgeServiceEFSqlServerTests(EFCoreSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        await _fixture.ClearAllDataAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public Task PurgeCycle_ExpiredAndLiveRows_RemovesOnlyTheProcessedExpiredRow() =>
        global::Encina.IntegrationTests.Messaging.MessagePurgeScenario.AssertCycleRemovesOnlyProcessedExpiredRowsAsync(
            global::Encina.IntegrationTests.Messaging.InboxPipelineHarnesses.EfSqlServer<TestEFDbContext>(_fixture));
}
