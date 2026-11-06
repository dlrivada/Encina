using Encina.TestInfrastructure.Fixtures;
using Xunit;

namespace Encina.IntegrationTests.Dapper.MySQL.Inbox;

/// <summary>
/// Runs the hosted <c>MessagePurgeService</c> end to end over the Dapper MySQL inbox store (#3046).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("Dapper-MySQL")]
public sealed class MessagePurgeServiceDapperTests : IAsyncLifetime
{
    private readonly MySqlFixture _fixture;

    public MessagePurgeServiceDapperTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ClearAllDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public Task PurgeCycle_ExpiredAndLiveRows_RemovesOnlyTheProcessedExpiredRow() =>
        global::Encina.IntegrationTests.Messaging.MessagePurgeScenario.AssertCycleRemovesOnlyProcessedExpiredRowsAsync(
            global::Encina.IntegrationTests.Messaging.InboxPipelineHarnesses.DapperMySql(_fixture));
}
