using Encina.TestInfrastructure.Fixtures;
using Xunit;

namespace Encina.IntegrationTests.Dapper.SqlServer.Inbox;

/// <summary>
/// Runs the hosted <c>MessagePurgeService</c> end to end over the Dapper SQL Server inbox store (#3046).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("Dapper-SqlServer")]
public sealed class MessagePurgeServiceDapperTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;

    public MessagePurgeServiceDapperTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ClearAllDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public Task PurgeCycle_ExpiredAndLiveRows_RemovesOnlyTheProcessedExpiredRow() =>
        global::Encina.IntegrationTests.Messaging.MessagePurgeScenario.AssertCycleRemovesOnlyProcessedExpiredRowsAsync(
            global::Encina.IntegrationTests.Messaging.InboxPipelineHarnesses.DapperSqlServer(_fixture));
}
