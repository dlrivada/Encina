using Encina.TestInfrastructure.Fixtures;
using Xunit;

namespace Encina.IntegrationTests.ADO.SqlServer.Inbox;

/// <summary>
/// Runs the hosted <c>MessagePurgeService</c> end to end over the ADO.NET SQL Server inbox store (#3046).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("ADO-SqlServer")]
public sealed class MessagePurgeServiceADOTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;

    public MessagePurgeServiceADOTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ClearAllDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public Task PurgeCycle_ExpiredAndLiveRows_RemovesOnlyTheProcessedExpiredRow() =>
        global::Encina.IntegrationTests.Messaging.MessagePurgeScenario.AssertCycleRemovesOnlyProcessedExpiredRowsAsync(
            global::Encina.IntegrationTests.Messaging.InboxPipelineHarnesses.AdoSqlServer(_fixture));
}
