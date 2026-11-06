using Encina.TestInfrastructure.Fixtures;
using Xunit;

namespace Encina.IntegrationTests.ADO.PostgreSQL.Inbox;

/// <summary>
/// Runs the hosted <c>MessagePurgeService</c> end to end over the ADO.NET PostgreSQL inbox store (#3046).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("ADO-PostgreSQL")]
public sealed class MessagePurgeServiceADOTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public MessagePurgeServiceADOTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ClearAllDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public Task PurgeCycle_ExpiredAndLiveRows_RemovesOnlyTheProcessedExpiredRow() =>
        global::Encina.IntegrationTests.Messaging.MessagePurgeScenario.AssertCycleRemovesOnlyProcessedExpiredRowsAsync(
            global::Encina.IntegrationTests.Messaging.InboxPipelineHarnesses.AdoPostgreSql(_fixture));
}
