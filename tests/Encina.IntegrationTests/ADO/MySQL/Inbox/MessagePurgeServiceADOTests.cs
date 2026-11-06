using Encina.TestInfrastructure.Fixtures;
using Xunit;

namespace Encina.IntegrationTests.ADO.MySQL.Inbox;

/// <summary>
/// Runs the hosted <c>MessagePurgeService</c> end to end over the ADO.NET MySQL inbox store (#3046).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("ADO-MySQL")]
public sealed class MessagePurgeServiceADOTests : IAsyncLifetime
{
    private readonly MySqlFixture _fixture;

    public MessagePurgeServiceADOTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ClearAllDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public Task PurgeCycle_ExpiredAndLiveRows_RemovesOnlyTheProcessedExpiredRow() =>
        global::Encina.IntegrationTests.Messaging.MessagePurgeScenario.AssertCycleRemovesOnlyProcessedExpiredRowsAsync(
            global::Encina.IntegrationTests.Messaging.InboxPipelineHarnesses.AdoMySql(_fixture));
}
